using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text.RegularExpressions;

using Rock.Attribute;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.CyberSource
{
    /// <summary>
    /// CyberSource Payment Gateway
    /// </summary>
    [Description( "CyberSource Payment Gateway" )]
    [Export( typeof( GatewayComponent ) )]
    [ExportMetadata( "ComponentName", "CyberSource" )]
    [TextField( "Merchant ID", "The CyberSource merchant ID (case-sensitive)", true, "", "", 0, "MerchantID" )]
    [MemoField( "Transaction Key", "The CyberSource transaction key", true, "", "", 0, "TransactionKey" )]
    [TextField( "Report User", "The CyberSource reporting user (case-sensitive)", true, "", "", 0, "ReportUser" )]
    [TextField( "Report Password", "The CyberSource reporting password (case-sensitive)", true, "", "", 0, "ReportPassword" )]
    [CustomRadioListField( "Mode", "Mode to use for transactions", "Live,Test", true, "Live", "", 4 )]
    [TimeField( "Batch Process Time", "The Batch processing cut-off time.  When batches are created by Rock, they will use this for the start/stop when creating new batches", false, "00:00:00", "", 5 )]
    public class Gateway : GatewayComponent
    {
        #region Gateway Component Implementation

        /// <summary>
        /// Gets the supported payment schedules.
        /// </summary>
        /// <value>
        /// The supported payment schedules.
        /// </value>
        public override List<DefinedValueCache> SupportedPaymentSchedules
        {
            get
            {
                var values = new List<DefinedValueCache>();
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_YEARLY ) );
                return values;
            }
        }

        /// <summary>
        /// Gets the batch time offset.
        /// </summary>
        public override TimeSpan BatchTimeOffset
        {
            get
            {
                var timeValue = new TimeSpan( 0 );
                if ( TimeSpan.TryParse( GetAttributeValue( "BatchProcessTime" ), out timeValue ) )
                {
                    return timeValue;
                }
                return base.BatchTimeOffset;
            }
        }

        /// <summary>
        /// Charges the specified payment info.
        /// </summary>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Charge( PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage request = GetMerchantInfo();
            request.billTo = GetBillTo( paymentInfo );
            request.item = GetItems( paymentInfo );
            request.purchaseTotals = GetTotals( paymentInfo );

            if ( paymentInfo is CreditCardPaymentInfo )
            {
                var cc = paymentInfo as CreditCardPaymentInfo;
                request.card = GetCard( cc );
            }
            else if ( paymentInfo is ACHPaymentInfo )
            {
                var ach = paymentInfo as ACHPaymentInfo;
                request.check = GetCheck( ach );
            }
            else if ( paymentInfo is ReferencePaymentInfo )
            {
                var reference = paymentInfo as ReferencePaymentInfo;
                request.recurringSubscriptionInfo = new RecurringSubscriptionInfo();
                request.recurringSubscriptionInfo.subscriptionID = reference.ReferenceNumber;
            }
            else
            {
                errorMessage = "Payment type not implemented.";
                return null;
            }

            if ( paymentInfo.CurrencyTypeValue.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) ) )
            {
                request.ccAuthService = new CCAuthService();
                request.ccAuthService.run = "true";
                request.ccAuthService.commerceIndicator = "internet";
                request.ccCaptureService = new CCCaptureService();
                request.ccCaptureService.run = "true";
            }
            else
            {
                request.ecDebitService = new ECDebitService();
                request.ecDebitService.run = "true";
                request.ecDebitService.commerceIndicator = "internet";
            }

            ReplyMessage reply = SubmitTransaction( request );
            if ( reply != null )
            {
                if ( reply.reasonCode.Equals( "100" ) )
                {
                    var transactionGuid = new Guid( reply.merchantReferenceCode );
                    var transaction = new FinancialTransaction { Guid = transactionGuid };
                    transaction.TransactionCode = reply.requestID;
                    return transaction;
                }
                else
                {
                    errorMessage = string.Format( "Your order was not approved.{0}", ProcessError( reply ) );
                }
            }
            else
            {
                errorMessage = "Invalid response from the financial gateway.";
            }

            return null;
        }

        /// <summary>
        /// Adds the scheduled payment.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialScheduledTransaction AddScheduledPayment( PaymentSchedule schedule, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage request = GetMerchantInfo();
            request.billTo = GetBillTo( paymentInfo );
            request.item = GetItems( paymentInfo );
            request.purchaseTotals = GetTotals( paymentInfo );
            request.recurringSubscriptionInfo = GetRecurring( schedule );
            request.recurringSubscriptionInfo.amount = paymentInfo.Amount.ToString();
            request.paySubscriptionCreateService = new PaySubscriptionCreateService();
            request.paySubscriptionCreateService.run = "true";

            if ( paymentInfo is CreditCardPaymentInfo )
            {
                var cc = paymentInfo as CreditCardPaymentInfo;
                request.card = GetCard( cc );
            }
            else if ( paymentInfo is ACHPaymentInfo )
            {
                var ach = paymentInfo as ACHPaymentInfo;
                request.check = GetCheck( ach );
            }
            else if ( paymentInfo is ReferencePaymentInfo )
            {
                var reference = paymentInfo as ReferencePaymentInfo;
                request.paySubscriptionCreateService.paymentRequestID = reference.TransactionCode;
            }
            else
            {
                errorMessage = "Payment type not implemented.";
                return null;
            }

            if ( !paymentInfo.CurrencyTypeValue.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) ) )
            {
                request.subscription = new Subscription();
                request.subscription.paymentMethod = "check";
            }

            ReplyMessage reply = SubmitTransaction( request );
            if ( reply != null )
            {
                if ( reply.reasonCode.Equals( "100" ) )
                {
                    var transactionGuid = new Guid( reply.merchantReferenceCode );
                    var scheduledTransaction = new FinancialScheduledTransaction { Guid = transactionGuid };
                    scheduledTransaction.TransactionCode = reply.paySubscriptionCreateReply.subscriptionID;

                    GetScheduledPaymentStatus( scheduledTransaction, out errorMessage );
                    return scheduledTransaction;
                }
                else
                {
                    errorMessage = string.Format( "Your order was not approved.{0}", ProcessError( reply ) );
                }
            }
            else
            {
                errorMessage = "Invalid response from the financial gateway.";
            }

            return null;
        }

        /// <summary>
        /// Updates the scheduled payment.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool UpdateScheduledPayment( FinancialScheduledTransaction transaction, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage request = GetMerchantInfo();
            request.recurringSubscriptionInfo = GetRecurring( transaction );
            request.paySubscriptionUpdateService = new PaySubscriptionUpdateService();
            request.paySubscriptionUpdateService.run = "true";

            if ( paymentInfo != null )
            {
                request.billTo = GetBillTo( paymentInfo );
                request.item = GetItems( paymentInfo );
                request.purchaseTotals = GetTotals( paymentInfo );
                request.recurringSubscriptionInfo.amount = paymentInfo.Amount.ToString();
            }

            ReplyMessage reply = SubmitTransaction( request );
            if ( reply != null )
            {
                if ( reply.reasonCode.Equals( "100" ) )
                {
                    var transactionGuid = new Guid( reply.merchantReferenceCode );
                    var scheduledTransaction = new FinancialScheduledTransaction { Guid = transactionGuid };
                    scheduledTransaction.TransactionCode = reply.paySubscriptionCreateReply.subscriptionID;

                    GetScheduledPaymentStatus( scheduledTransaction, out errorMessage );
                    return true;
                }
                else
                {
                    errorMessage = string.Format( "Unable to update this transaction. {0}", ProcessError( reply ) );
                }
            }
            else
            {
                errorMessage = "Invalid response from the financial gateway.";
            }

            return false;
        }

        /// <summary>
        /// Cancels the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool CancelScheduledPayment( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage request = GetMerchantInfo();
            request.recurringSubscriptionInfo = GetRecurring( transaction );
            request.paySubscriptionDeleteService = new PaySubscriptionDeleteService();
            request.paySubscriptionDeleteService.run = "true";

            ReplyMessage reply = SubmitTransaction( request );
            if ( reply != null )
            {
                if ( reply.reasonCode.Equals( "100" ) )
                {
                    return true;
                }
                else
                {
                    errorMessage = string.Format( "Unable to cancel this transaction. {0}", ProcessError( reply ) );
                }
            }
            else
            {
                errorMessage = "Invalid response from the financial gateway.";
            }

            return false;
        }

        /// <summary>
        /// Gets the scheduled payment status.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool GetScheduledPaymentStatus( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage verifyRequest = GetMerchantInfo();
            verifyRequest.paySubscriptionRetrieveService = new PaySubscriptionRetrieveService();
            verifyRequest.paySubscriptionRetrieveService.run = "true";
            verifyRequest.recurringSubscriptionInfo = new RecurringSubscriptionInfo();
            verifyRequest.recurringSubscriptionInfo.subscriptionID = transaction.TransactionCode;

            ReplyMessage verifyReply = SubmitTransaction( verifyRequest );
            if ( verifyReply.reasonCode.Equals( "100" ) )
            {
                transaction.IsActive = verifyReply.paySubscriptionRetrieveReply.status.ToUpper() == "CURRENT";
                var startDate = GetDate( verifyReply.paySubscriptionRetrieveReply.startDate );
                transaction.StartDate = startDate ?? transaction.StartDate;
                transaction.NextPaymentDate = NextPaymentDate( startDate, verifyReply.paySubscriptionRetrieveReply.frequency ) ?? transaction.NextPaymentDate;
                transaction.NumberOfPayments = verifyReply.paySubscriptionRetrieveReply.totalPayments.AsInteger() ?? transaction.NumberOfPayments;
                transaction.LastStatusUpdateDateTime = DateTime.Now;

                return true;
            }
            else
            {
                errorMessage = ProcessError( verifyReply );
            }

            return false;
        }

        /// <summary>
        /// Gets the payments that have been processed for any scheduled transactions
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override List<Payment> GetPayments( DateTime startDate, DateTime endDate, out string errorMessage )
        {
            errorMessage = string.Empty;
            List<Payment> paymentList = new List<Payment>();
            var reportingApi = new Reporting.Api(
                GetAttributeValue( "MerchantID" ),
                GetAttributeValue( "TransactionKey" ),
                GetAttributeValue( "ReportUser" ),
                GetAttributeValue( "ReportPassword" ),
                GetAttributeValue( "Mode" ).Equals( "Live", StringComparison.CurrentCultureIgnoreCase )
            );

            TimeSpan timeDifference = endDate - startDate;
            for ( int offset = 0; offset <= timeDifference.TotalDays; offset++ )
            {
                DateTime offsetDate = startDate.AddDays( offset ) < endDate ? startDate.AddDays( offset ) : endDate;
                
                var reportParams = new Dictionary<string, string>();
                reportParams.Add( "date", offsetDate.ToString( "YYYY/MM/DD" ) );

                DataTable dt = reportingApi.GetReport( "SubscriptionDetailReport", reportParams, out errorMessage );
                if ( dt != null )
                {
                    var transactions = new List<Payment>();

                    paymentList.AddRange( transactions );
                }
            }

            if ( paymentList.Any() )
            {                
                return paymentList;
            }
            else
            {
                errorMessage = "The subscription detail report did not return any data";
                return null;
            }            
        }

        /// <summary>
        /// Gets the reference number from the gateway for converting a transaction to a profile.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber( FinancialTransaction transaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            RequestMessage request = GetMerchantInfo();
            request.billTo = GetBillTo( transaction );
            request.recurringSubscriptionInfo = new RecurringSubscriptionInfo();
            request.recurringSubscriptionInfo.frequency = "ON-DEMAND";
            request.recurringSubscriptionInfo.amount = "0";
            request.paySubscriptionCreateService = new PaySubscriptionCreateService();
            request.paySubscriptionCreateService.run = "true";
            request.paySubscriptionCreateService.paymentRequestID = transaction.TransactionCode;

            ReplyMessage reply = SubmitTransaction( request );
            if ( reply.reasonCode == "100" )
            {
                return reply.paySubscriptionCreateReply.subscriptionID;
            }
            else
            {
                errorMessage = ProcessError( reply );
            }

            return string.Empty;
        }

        #endregion

        #region Process Transaction

        /// <summary>
        /// Submits the transaction.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private ReplyMessage SubmitTransaction( RequestMessage request )
        {
            ReplyMessage reply = new ReplyMessage();
            string merchantID = GetAttributeValue( "MerchantID" );
            string transactionkey = GetAttributeValue( "TransactionKey" );

            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Name = "ITransactionProcessor";
            binding.MaxBufferSize = 2147483647;
            binding.MaxBufferPoolSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.ReaderQuotas.MaxDepth = 2147483647;
            binding.ReaderQuotas.MaxArrayLength = 2147483647;
            binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            binding.Security.Mode = BasicHttpSecurityMode.TransportWithMessageCredential;
            EndpointAddress address = new EndpointAddress( new Uri( GatewayUrl ) );

            var proxy = new TransactionProcessorClient( binding, address );
            proxy.ClientCredentials.UserName.UserName = merchantID;
            proxy.ClientCredentials.UserName.Password = transactionkey;
            proxy.Endpoint.Address = address;
            proxy.Endpoint.Binding = binding;

            try
            {
                reply = proxy.runTransaction( request );
                return reply;
            }
            catch ( TimeoutException e )
            {
                reply.reasonCode = "151";
                reply.additionalData = e.ToString();
                return reply;
            }
            catch ( FaultException e )
            {
                reply.reasonCode = "150";
                reply.additionalData = e.ToString();
                return reply;
            }
            catch ( WebException we )
            {
                reply.reasonCode = "";
                reply.additionalData = we.ToString();
                return reply;
            }
        }

        /// <summary>
        /// Processes the error message.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <returns></returns>
        private string ProcessError( ReplyMessage reply )
        {
            int reasonCode = int.Parse( reply.reasonCode );
            switch ( reasonCode )
            {
                // Missing field or fields
                case 101:
                    return "\nThe following required fields are missing: " + string.Join( "\n", reply.missingField );
                // Invalid field or fields
                case 102:
                    return "\nThe following fields are invalid: " + string.Join( "\n", reply.invalidField );
                // Partial payment approved
                case 110:
                    return "\nOnly a partial amount of this transaction was approved.";
                // General system failure
                case 150:
                    return "\nThe payment processor did not process your payment.";
                // System timeout
                case 151:
                    return "\nThe payment request timed out.";
                // Service request timed out
                case 152:
                    return "\nThe payment service timed out.";
                // AVS check failed
                case 200:
                    return "\nThe payment billing address did not match the bank's address on record. Please verify your address details.";
                // Expired card
                case 202:
                    return "\nThe card has expired. Please use a different card or select another form of payment.";
                // Card declined
                case 203:
                    return "\nThe card was declined without a reason given. Please use a different card or select another form of payment.";
                // Insufficient funds
                case 204:
                    return "\nInsufficient funds in the account. Please use another form of payment.";
                // Stolen card
                case 205:
                    return "\nThe card has been reported stolen. Please use a different card or select another form of payment.";
                // Bank unavailable
                case 207:
                    return "\nThe bank processor is temporarily unavailable. Please try again in a few minutes.";
                // Card not active
                case 208:
                    return "\nThe card is inactive or not authorized for internet transactions. Please use a different card or select another form of payment.";
                // AmEx invalid CID
                case 209:
                    return "\nThe card identification digit did not match.  Please use a different card or select another form of payment.";
                // Maxed out
                case 210:
                    return "\nThe card has reached its credit limit.  Please use a different card or select another form of payment.";
                // Invalid verification #
                case 211:
                    return "\nThe card verification number is invalid. Please verify your 3 or 4 digit verification number.";
                // Frozen account
                case 222:
                    return "\nThe selected account has been frozen. Please use another form of payment.";
                // Invalid verification #
                case 230:
                    return "\nThe card verification number is invalid. Please verify your 3 or 4 digit verification number.";
                // Invalid account #
                case 231:
                    return "\nThe account number is invalid. Please use another form of payment.";
                // Invalid merchant config
                case 234:
                    return "\nThe merchant configuration is invalid. Please contact CyberSource customer support.";
                // Processor failure
                case 236:
                    return "\nThe payment processor is offline. Please try again in a few minutes.";
                // Card type not accepted
                case 240:
                    return "\nThe card type is not accepted by the merchant. Please use another form of payment.";
                // Payment processor timeout
                case 250:
                    return "\nThe payment request was received but has not yet been processed.";
                // Any others not identified
                default:
                    return "\nYour payment was not processed.  Please double check your payment details.";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the gateway URL.
        /// </summary>
        /// <value>
        /// The gateway URL.
        /// </value>
        private string GatewayUrl
        {
            get
            {
                if ( GetAttributeValue( "Mode" ).Equals( "Live", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    return "https://ics2ws.ic3.com/commerce/1.x/transactionProcessor/CyberSourceTransaction_1.91.wsdl";
                }
                else
                {
                    return "https://ics2wstest.ic3.com/commerce/1.x/transactionProcessor/CyberSourceTransaction_1.91.wsdl";
                }
            }
        }

        /// <summary>
        /// Gets the merchant information.
        /// </summary>
        /// <returns></returns>
        private RequestMessage GetMerchantInfo()
        {
            RequestMessage request = new RequestMessage();

            request.merchantID = GetAttributeValue( "MerchantID" );
            request.merchantReferenceCode = Guid.NewGuid().ToString();
            request.clientLibraryVersion = Environment.Version.ToString();
            request.clientApplication = VersionInfo.VersionInfo.GetRockProductVersionFullName();
            request.clientApplicationVersion = VersionInfo.VersionInfo.GetRockProductVersionNumber();
            request.clientApplicationUser = GetAttributeValue( "OrganizationName" );
            request.clientEnvironment =
                Environment.OSVersion.Platform +
                Environment.OSVersion.Version.ToString() + "-CLR" +
                Environment.Version.ToString();

            return request;
        }

        /// <summary>
        /// Gets the billing details.
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private BillTo GetBillTo( PaymentInfo paymentInfo )
        {
            BillTo billingInfo = new BillTo();
            billingInfo.firstName = paymentInfo.FirstName.Left( 50 );       // up to 50 chars
            billingInfo.lastName = paymentInfo.LastName.Left( 60 );         // up to 60 chars
            billingInfo.email = paymentInfo.Email;                          // up to 255 chars
            billingInfo.phoneNumber = paymentInfo.Phone.Left( 15 );         // up to 15 chars
            billingInfo.street1 = paymentInfo.Street.Left( 50 );            // up to 50 chars
            billingInfo.city = paymentInfo.City.Left( 50 );                 // up to 50 chars
            billingInfo.state = paymentInfo.State.Left( 2 );                // only 2 chars
            billingInfo.postalCode = paymentInfo.Zip.Length > 5
                ? Regex.Replace( paymentInfo.Zip, @"^(.{5})(.{4})$", "$1-$2" )
                : paymentInfo.Zip;                                          // 9 chars with a separating -

            billingInfo.country = "US";                                     // only 2 chars
            billingInfo.ipAddress = Dns.GetHostEntry( Dns.GetHostName() )
                .AddressList.FirstOrDefault( ip => ip.AddressFamily == AddressFamily.InterNetwork ).ToString();

            if ( paymentInfo is CreditCardPaymentInfo )
            {
                var cc = paymentInfo as CreditCardPaymentInfo;
                billingInfo.street1 = cc.BillingStreet.Left( 50 );
                billingInfo.city = cc.BillingCity.Left( 50 );
                billingInfo.state = cc.BillingState.Left( 2 );
                billingInfo.postalCode = cc.BillingZip.Left( 10 );
            }

            return billingInfo;
        }

        /// <summary>
        /// Gets the bill to.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns></returns>
        private BillTo GetBillTo( FinancialTransaction transaction )
        {
            BillTo billingInfo = new BillTo();
            billingInfo.customerID = transaction.AuthorizedPerson.Id.ToString();
            billingInfo.firstName = transaction.AuthorizedPerson.FirstName.Left( 50 );       // up to 50 chars
            billingInfo.lastName = transaction.AuthorizedPerson.LastName.Left( 50 );       // up to 60 chars
            billingInfo.email = transaction.AuthorizedPerson.Email.Left( 255 );              // up to 255 chars
            billingInfo.ipAddress = Dns.GetHostEntry( Dns.GetHostName() )
                .AddressList.FirstOrDefault( ip => ip.AddressFamily == AddressFamily.InterNetwork ).ToString();

            return billingInfo;
        }

        /// <summary>
        /// Gets the payment items.
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private Item[] GetItems( PaymentInfo paymentInfo )
        {   // just get a single item for the total amount
            List<Item> itemList = new List<Item>();

            Item item = new Item();
            item.id = "0";
            item.unitPrice = paymentInfo.Amount.ToString();
            item.totalAmount = paymentInfo.Amount.ToString();
            itemList.Add( item );
            return itemList.ToArray();
        }

        /// <summary>
        /// Gets the purchase totals.
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private PurchaseTotals GetTotals( PaymentInfo paymentInfo )
        {
            PurchaseTotals purchaseTotals = new PurchaseTotals();
            purchaseTotals.currency = "USD";
            return purchaseTotals;
        }

        /// <summary>
        /// Gets the card information.
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private Card GetCard( CreditCardPaymentInfo cc )
        {
            var card = new Card();
            card.accountNumber = cc.Number.AsNumeric();
            card.expirationMonth = cc.ExpirationDate.Month.ToString( "D2" );
            card.expirationYear = cc.ExpirationDate.Year.ToString( "D4" );
            card.cvNumber = cc.Code.AsNumeric();
            card.cvIndicator = "1";

            switch ( cc.CreditCardTypeValue.Name )
            {
                case "Visa":
                    card.cardType = "001";
                    break;
                case "MasterCard":
                    card.cardType = "002";
                    break;
                case "American Express":
                    card.cardType = "003";
                    break;
                case "Discover":
                    card.cardType = "004";
                    break;
                case "Diners":
                    card.cardType = "005";
                    break;
                case "Carte Blanche":
                    card.cardType = "006";
                    break;
                case "JCB":
                    card.cardType = "007";
                    break;
                default:
                    card.cardType = string.Empty;
                    break;
            }

            return card;
        }

        /// <summary>
        /// Gets the check information.
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private Check GetCheck( ACHPaymentInfo ach )
        {
            var check = new Check();
            check.accountNumber = ach.BankAccountNumber.AsNumeric();
            check.accountType = ach.AccountType == BankAccountType.Checking ? "C" : "S";
            check.bankTransitNumber = ach.BankRoutingNumber.AsNumeric();
            check.secCode = "WEB";

            return check;
        }

        /// <summary>
        /// Gets the recurring subscription info.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        /// <returns></returns>
        private RecurringSubscriptionInfo GetRecurring( PaymentSchedule schedule )
        {
            var recurringSubscriptionInfo = new RecurringSubscriptionInfo();
            recurringSubscriptionInfo.startDate = schedule.StartDate.ToString( "yyyyMMdd" );
            recurringSubscriptionInfo.automaticRenew = "true";

            SetPayPeriod( recurringSubscriptionInfo, schedule.TransactionFrequencyValue );

            return recurringSubscriptionInfo;
        }

        /// <summary>
        /// Gets the recurring subscription info.
        /// </summary>
        /// <param name="transaction">The schedule.</param>
        /// <returns></returns>
        private RecurringSubscriptionInfo GetRecurring( FinancialScheduledTransaction transaction )
        {
            var recurringSubscriptionInfo = new RecurringSubscriptionInfo();
            recurringSubscriptionInfo.subscriptionID = transaction.TransactionCode;
            recurringSubscriptionInfo.startDate = transaction.StartDate.ToString( "yyyyMMdd" );
            recurringSubscriptionInfo.automaticRenew = "true";

            if ( transaction.TransactionFrequencyValueId > 0 )
            {
                SetPayPeriod( recurringSubscriptionInfo, DefinedValueCache.Read( transaction.TransactionFrequencyValueId ) );
            }

            return recurringSubscriptionInfo;
        }

        /// <summary>
        /// Sets the pay period.
        /// </summary>
        /// <param name="recurringSubscriptionInfo">The recurring subscription information.</param>
        /// <param name="transactionFrequencyValue">The transaction frequency value.</param>
        private void SetPayPeriod( RecurringSubscriptionInfo recurringSubscriptionInfo, DefinedValueCache transactionFrequencyValue )
        {
            var selectedFrequencyGuid = transactionFrequencyValue.Guid.ToString().ToUpper();
            switch ( selectedFrequencyGuid )
            {
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME:
                    recurringSubscriptionInfo.frequency = "ON-DEMAND";
                    recurringSubscriptionInfo.amount = "0";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY:
                    recurringSubscriptionInfo.frequency = "WEEKLY";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY:
                    recurringSubscriptionInfo.frequency = "BI-WEEKLY";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY:
                    var nextValidDate = DateTime.ParseExact( recurringSubscriptionInfo.startDate, "yyyyMMdd", null ).AddDays( -1 );
                    nextValidDate = nextValidDate.Day >= 15
                        ? nextValidDate = new DateTime( nextValidDate.Year, nextValidDate.Month, 1 ).AddMonths( 1 )
                        : nextValidDate = new DateTime( nextValidDate.Year, nextValidDate.Month, 15 );
                    recurringSubscriptionInfo.startDate = nextValidDate.ToString( "yyyyMMdd" );
                    recurringSubscriptionInfo.frequency = "SEMI-MONTHLY";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY:
                    recurringSubscriptionInfo.frequency = "MONTHLY";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_YEARLY:
                    recurringSubscriptionInfo.frequency = "ANNUALLY";
                    break;
            }
        }

        /// <summary>
        /// Gets the next payment date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="frequency">The frequency.</param>
        /// <returns></returns>
        private DateTime? NextPaymentDate( DateTime? dt, string frequency )
        {
            DateTime startDate = (DateTime)( dt ?? DateTime.Now );
            DateTime nextDate;
            switch ( frequency.ToUpper() )
            {
                case "WEEKLY":
                    nextDate = startDate.AddDays( 7 );
                    break;
                case "BI-WEEKLY":
                    nextDate = startDate.AddDays( 14 );
                    break;
                case "SEMI-MONTHLY":
                    nextDate = startDate.Day >= 15 ? new DateTime( startDate.Year, startDate.Month, 1 ).AddMonths( 1 )
                        : new DateTime( startDate.Year, startDate.Month, 15 );
                    break;
                case "MONTHLY":
                    nextDate = startDate.AddMonths( 1 );
                    break;
                case "ANNUALLY":
                    nextDate = startDate.AddYears( 1 );
                    break;
                default:
                    nextDate = startDate;
                    break;
            }

            return nextDate;
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        private DateTime? GetDate( string date )
        {
            DateTime dt = DateTime.MinValue;
            if ( DateTime.TryParseExact( date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out dt ) )
            {
                return dt;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
