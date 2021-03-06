﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Sql.Properties;
using Microsoft.Azure.Commands.Sql.Security.Model;
using Microsoft.Azure.Commands.Sql.Security.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Azure.Commands.Sql.Security.Cmdlet.DataMasking
{
    /// <summary>
    /// Base for creation and update of data masking rule.
    /// </summary>
    public abstract class BuildAzureSqlDatabaseDataMaskingRule : SqlDatabaseDataMaskingRuleCmdletBase
    {
        /// <summary>
        /// The name of the parameter set for data masking rule that specifies table and column names
        /// </summary>
        internal const string ByTableAndColumn = "ByTableAndColumn";

        /// <summary>
        /// The name of the parameter set for data masking rule that specifies alias
        /// </summary>
        internal const string ByAlias = "ByAlias";

        /// <summary>
        /// Gets or sets the table name
        /// </summary>
        [Parameter(ParameterSetName = ByTableAndColumn, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The table name.")]
        [ValidateNotNullOrEmpty]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        [Parameter(ParameterSetName = ByTableAndColumn, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The column name.")]
        [ValidateNotNullOrEmpty]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the alias name
        /// </summary>
        [Parameter(ParameterSetName = ByAlias, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The alias name.")]
        [ValidateNotNullOrEmpty]
        public string AliasName { get; set; }

        /// <summary>
        /// Gets or sets the masking function - the definition of this property as a cmdlet parameter is done in the subclasses
        /// </summary>
        public virtual string MaskingFunction { get; set; }

        /// <summary>
        /// Gets or sets the prefix size when using the text masking function
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The prefix size when using the text masking function.")]
        public uint? PrefixSize { get; set; }

        /// <summary>
        /// Gets or sets the replacement string when using the text masking function
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The replacement string when using the text masking function.")]
        public string ReplacementString { get; set; }

        /// <summary>
        /// Gets or sets the suffix size when using the text masking function
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The suffix size string when using the text masking function.")]
        public uint? SuffixSize { get; set; }

        /// <summary>
        /// Gets or sets the NumberFrom property, which is the lower bound of the random interval when using the number masking function
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The lower bound of the random interval when using the number masking function.")]
        public double? NumberFrom { get; set; }

        /// <summary>
        /// Gets or sets the NumberTo property, which is the upper bound of the random interval when using the number masking function
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The upper bound of the random interval when using the number masking function.")]
        public double? NumberTo { get; set; }

        /// <summary>
        ///  Defines whether the cmdlets will output the model object at the end of its execution
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Updates the given model element with the cmdlet specific operation 
        /// </summary>
        /// <param name="model">A model object</param>
        protected override IEnumerable<DatabaseDataMaskingRuleModel> UpdateModel(IEnumerable<DatabaseDataMaskingRuleModel> rules)
        {
            string errorMessage = ValidateRuleTarget(rules);
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = ValidateOperation(rules);
            }
            
            if(!string.IsNullOrEmpty(errorMessage))
            {
                throw new Exception(errorMessage);
            }
            DatabaseDataMaskingRuleModel rule = GetRule(rules);
            DatabaseDataMaskingRuleModel updatedRule = UpdateRule(rule);
            return UpdateRuleList(rules, rule);
        }

        /// <summary>
        /// Validation that the rule's target is set properly to be either a table and column for which there's no other rule, or an alias for which there's no other rule.
        /// </summary>
        /// <param name="rules">The data masking rules of the current database</param>
        /// <returns>A string containing error message or null in case all is fine</returns>
        protected string ValidateRuleTarget(IEnumerable<DatabaseDataMaskingRuleModel> rules)
        {
            if (AliasName != null) // using the alias parameter set
            {
                if(rules.Any(r => r.AliasName == AliasName && r.RuleId != RuleId))
                {
                    return string.Format(CultureInfo.InvariantCulture, Resources.DataMaskingAliasAlreadyUsedError, AliasName);
                }
            }
            else
            {
                if (rules.Any(r => r.TableName == TableName && r.ColumnName == ColumnName && r.RuleId != RuleId))
                {
                    return string.Format(CultureInfo.InvariantCulture, Resources.DataMaskingTableAndColumnUsedError, TableName, ColumnName);
                }
            }
            return null;
        }

        /// <summary>
        /// Update the rule this cmdlet is operating on based on the values provided by the user
        /// </summary>
        /// <param name="rule">The rule this cmdlet operates on</param>
        /// <returns>An updated rule model</returns>
        protected DatabaseDataMaskingRuleModel UpdateRule(DatabaseDataMaskingRuleModel rule)
        {
            if(!string.IsNullOrEmpty(AliasName))
            {
                rule.AliasName = AliasName;
                rule.TableName = null;
                rule.ColumnName = null;
            }
            else
            {
                rule.TableName = TableName;
                rule.ColumnName = ColumnName;
                rule.AliasName = null;
            }

            if(!string.IsNullOrEmpty(MaskingFunction)) // only update if the user provided this value
            {
                rule.MaskingFunction = ModelizeMaskingFunction();
            }

            if(rule.MaskingFunction == Model.MaskingFunction.Text)
            {
                if (PrefixSize != null) // only update if the user provided this value
                {
                    rule.PrefixSize = PrefixSize;
                }

                if (!string.IsNullOrEmpty(ReplacementString)) // only update if the user provided this value
                {
                    rule.ReplacementString = ReplacementString;
                }

                if (SuffixSize != null) // only update if the user provided this value
                {
                    rule.SuffixSize = SuffixSize;
                }

                if(rule.PrefixSize == null)
                {
                    rule.PrefixSize = Constants.PrefixSizeDefaultValue;
                }

                if (string.IsNullOrEmpty(rule.ReplacementString))
                {
                    rule.ReplacementString = Constants.ReplacementStringDefaultValue;
                }

                if (rule.SuffixSize == null)
                {
                    rule.SuffixSize = Constants.SuffixSizeDefaultValue;
                }
            }

            if (rule.MaskingFunction == Model.MaskingFunction.Number)
            {
                if (NumberFrom != null) // only update if the user provided this value
                {
                    rule.NumberFrom = NumberFrom;
                }

                if (NumberTo != null) // only update if the user provided this value
                {
                    rule.NumberTo = NumberTo;
                }

                if (rule.NumberFrom == null)
                {
                    rule.NumberFrom = Constants.NumberFromDefaultValue;
                }

                if (rule.NumberTo == null)
                {
                    rule.NumberTo = Constants.NumberToDefaultValue;
                }

                if(rule.NumberFrom > rule.NumberTo)
                {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, Resources.DataMaskingNumberRuleIntervalDefinitionError));
                }
            }
            return rule;
        }

        /// <summary>
        /// Transforms the user given data masking function to its model representation
        /// </summary>
        /// <returns>The model representation of the user provided masking function</returns>
        private MaskingFunction ModelizeMaskingFunction()
        {
            if (MaskingFunction == Constants.CCN) return Model.MaskingFunction.CreditCardNumber;
            if (MaskingFunction == Constants.NoMasking) return Model.MaskingFunction.NoMasking;
            if (MaskingFunction == Constants.Number) return Model.MaskingFunction.Number;
            if (MaskingFunction == Constants.Text) return Model.MaskingFunction.Text;
            if (MaskingFunction == Constants.Email) return Model.MaskingFunction.Email;
            if (MaskingFunction == Constants.SSN) return Model.MaskingFunction.SocialSecurityNumber;
            return Model.MaskingFunction.Default;
        }

        /// <summary>
        /// An additional validation hook for inheriting classes to provide specific validation.
        /// </summary>
        /// <param name="rules">The rule the cmdlet operates on</param>
        /// <returns>An error message or null if all is fine</returns>
        protected abstract string ValidateOperation(IEnumerable<DatabaseDataMaskingRuleModel> rules);

        /// <summary>
        /// Returns the rule that this cmdlet operates on
        /// </summary>
        /// <param name="rules">All the data masking rules of the database</param>
        /// <returns>The rule that this cmdlet operates on</returns>
        protected abstract DatabaseDataMaskingRuleModel GetRule(IEnumerable<DatabaseDataMaskingRuleModel> rules);

        /// <summary>
        /// Update the rule that this cmdlet operates on based on the user provided values
        /// </summary>
        /// <param name="rules">The data masking rules of the database</param>
        /// <param name="rule">The rule that this cmdlet operates on</param>
        /// <returns>The update list of the database's data masking rules</returns>
        protected abstract IEnumerable<DatabaseDataMaskingRuleModel> UpdateRuleList(IEnumerable<DatabaseDataMaskingRuleModel> rules, DatabaseDataMaskingRuleModel rule);

        /// <summary>
        /// This method is responsible to call the right API in the communication layer that will eventually send the information in the 
        /// object to the REST endpoint
        /// </summary>
        /// <param name="model">The model object with the data to be sent to the REST endpoints</param>
        protected override void SendModel(IEnumerable<DatabaseDataMaskingRuleModel> rules)
        {
            ModelAdapter.SetDatabaseDataMaskingRule(rules.First(r => r.RuleId == RuleId), clientRequestId);
        }

        /// <summary>
        /// Returns true if the model object that was constructed by this cmdlet should be written out
        /// </summary>
        /// <returns>True if the model object should be written out, False otherwise</returns>
        protected override bool WriteResult() { return PassThru; }
    }
}