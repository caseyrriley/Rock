//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//

using System;
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// FinancialPersonSavedAccount Service class
    /// </summary>
    public partial class FinancialPersonSavedAccountService : Service<FinancialPersonSavedAccount>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FinancialPersonSavedAccountService"/> class
        /// </summary>
        public FinancialPersonSavedAccountService()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FinancialPersonSavedAccountService"/> class
        /// </summary>
        public FinancialPersonSavedAccountService(IRepository<FinancialPersonSavedAccount> repository) : base(repository)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( FinancialPersonSavedAccount item, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class FinancialPersonSavedAccountExtensionMethods
    {
        /// <summary>
        /// Clones this FinancialPersonSavedAccount object to a new FinancialPersonSavedAccount object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static FinancialPersonSavedAccount Clone( this FinancialPersonSavedAccount source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as FinancialPersonSavedAccount;
            }
            else
            {
                var target = new FinancialPersonSavedAccount();
                target.PersonId = source.PersonId;
                target.GatewayId = source.GatewayId;
                target.Name = source.Name;
                target.PaymentMethod = source.PaymentMethod;
                target.MaskedAccountNumber = source.MaskedAccountNumber;
                target.TransactionCode = source.TransactionCode;
                target.Id = source.Id;
                target.Guid = source.Guid;

            
                return target;
            }
        }
    }
}