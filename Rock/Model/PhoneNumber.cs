﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Phone Number POCO Entity.
    /// </summary>
    [Table( "PhoneNumber" )]
    [DataContract]
    public partial class PhoneNumber : Model<PhoneNumber>
    {
        /// <summary>
        /// Gets or sets a flag indicating if the PhoneNumber is part of the Rock core system/framework. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean" /> value that is <c>true</c> if the PhoneNumber is part of the Rock core system/framework; otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }
        
        /// <summary>
        /// Gets or sets the PersonId of the <see cref="Rock.Model.Person" /> that the PhoneNumber belongs to. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the PersonId of the <see cref="Rock.Model.Person"/> that the PhoneNumber belongs to.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int PersonId { get; set; }
        
        /// <summary>
        /// Gets or sets the phone number. The number is stored without any string formatting. (i.e. (502) 555-1212 will be stored as 5025551212). This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the phone number without string formatting.
        /// </value>
        [Required]
        [MaxLength( 20 )]
        [DataMember( IsRequired = true )]
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the extension (if any) that would need to be dialed to contact the owner. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the extensions that would need to be dialed to contact the owner. If no extension is required, this property will be null. 
        /// </value>
        [MaxLength( 20 )]
        [DataMember]
        public string Extension { get; set; }

        /// <summary>
        /// Gets the Phone Number's Number Type <see cref="Rock.Model.DefinedValue"/> Id.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the Number Type <see cref="Rock.Model.DefinedValue"/> Id. If unknown, this value will be null.
        /// </value>
        [DataMember]
        public int? NumberTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the number has been opted in for SMS
        /// </summary>
        /// <value>
        ///   A <see cref="System.Boolean"/> value that is <c>true</c> if the phone number has opted in for SMS messaging; otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsMessagingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the Phone Number's NumberType <see cref="Rock.Model.DefinedValue"/>
        /// </summary>
        /// <value>
        /// The Number Type <see cref="Rock.Model.DefinedValue"/> of the phone number.
        /// </value>
        [DataMember]
        public virtual Model.DefinedValue NumberTypeValue { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the PhoneNumber is unlisted or not.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if the PhoneNumber is unlisted; otherwise <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsUnlisted { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the PhoneNumber.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing an optional description of the PhoneNumber.
        /// </value>
        [DataMember]
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Person"/> who the PhoneNumber belongs to.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Person"/> that the phone number belongs to.
        /// </value>
        [DataMember]
        public virtual Person Person { get; set; }

        /// <summary>
        /// Formats a provided string of numbers as a US phone number in (###) ###-#### format. 
        /// </summary>
        /// <param name="number">A <see cref="System.String"/> containing the number to format.</param>
        /// <returns>A <see cref="System.String"/> containing the number in ###-#### or (###) ###-#### format. If more than 10 digits are provided all digits after the 10th position will be formatted as an extension. </returns>
        public static string FormattedNumber( string number )
        {
            if ( string.IsNullOrWhiteSpace( number ) )
            {
                return string.Empty;
            }

            number = new System.Text.RegularExpressions.Regex( @"\D" ).Replace( number, string.Empty );
            number = number.TrimStart( '1' );
            if ( number.Length == 7 )
                return Convert.ToInt64( number ).ToString( "###-####" );
            if ( number.Length == 10 )
                return Convert.ToInt64( number ).ToString( "(###) ###-####" );
            if ( number.Length > 10 )
                return Convert.ToInt64( number )
                    .ToString( "(###) ###-#### " + new String( '#', ( number.Length - 10 ) ) );
            return number;
        }

        /// <summary>
        /// Removes non-numeric characters from a provided number
        /// </summary>
        /// <param name="number">A <see cref="System.String"/> containing the phone number to clean.</param>
        /// <returns>A <see cref="System.String"/> containing the phone number with all non numeric characters removed. </returns>
        public static string CleanNumber( string number )
        {
            return digitsOnly.Replace(number, "");
        }
        private static Regex digitsOnly = new Regex( @"[^\d]" );

        /// <summary>
        /// Returns a formatted version of the Number.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> containing the formatted number.
        /// </value>
        public virtual string NumberFormatted
        {
            get { return PhoneNumber.FormattedNumber( Number ); }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> containing the Number and represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> containing the Number and represents this instance.
        /// </returns>
        public override string ToString()
        {
            return FormattedNumber( this.Number );
        }
    }

    /// <summary>
    /// Phone Number Configuration class.
    /// </summary>
    public partial class PhoneNumberConfiguration : EntityTypeConfiguration<PhoneNumber>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneNumberConfiguration"/> class.
        /// </summary>
        public PhoneNumberConfiguration()
        {
            this.HasRequired( p => p.Person ).WithMany( p => p.PhoneNumbers ).HasForeignKey( p => p.PersonId ).WillCascadeOnDelete(false);
            this.HasOptional( p => p.NumberTypeValue ).WithMany().HasForeignKey( p => p.NumberTypeValueId ).WillCascadeOnDelete( false );
        }
    }
}
