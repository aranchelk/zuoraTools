using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using zuoraTools.zuora;

namespace zuoraTools
{
    class ZObjectHelper
    {
        public static List<string> SupportedZObjects 
        {
            get { return _constructorDict.Keys.ToList(); }
        }
        private static Dictionary<string, Func<zObject>> _constructorDict = new Dictionary<string, Func<zObject>>()
            {
                { "account", () => new Account() },
                { "accountingperiod", () => new AccountingPeriod() },

                { "amendment", () => new Amendment() },
                { "contact", () => new Contact() },
                { "creditbalanceadjustment", () => new CreditBalanceAdjustment() },

                { "invoice", () => new Invoice() },
                { "invoiceItem", () => new InvoiceItem() },
                { "invoiceitemadjustment", () => new InvoiceItemAdjustment() },
                { "invoicepayment", () => new InvoicePayment() },
                //{ "invoiceadjustment", () => new InvoiceAdjustment() },

                { "payment", () => new Payment() },
                { "paymentmethod", () => new PaymentMethod() },

                { "product", () => new Product() },
                { "productrateplan", () => new ProductRatePlan() },
                { "productrateplancharge", () => new ProductRatePlanCharge() },
                { "productrateplanchargetier", () => new ProductRatePlanChargeTier() },

                { "rateplan", () => new RatePlan() },

                { "rateplancharge", () => new RatePlanCharge() },
                { "rateplanchargetier", () => new RatePlanChargeTier() },
                { "refund", () => new Refund() },
                { "refundinvoicepayment", () => new RefundInvoicePayment() },
                
                { "subscription", () => new Subscription() },

                { "taxationitem", () => new TaxationItem() },
                { "usage", () => new Usage() },               
            };
        //This class contains helper methods for zObjects, primarily to convert them to and from CSV data.
        //It can compose zObjects from dictionary text with case insensitive field names

        public static string ToCsvRecord(zObject o, string[] propertiesCaseInsensitive)
        {
            string[] properties = FixPropertyCases(o, propertiesCaseInsensitive);
            List<string> recordData = new List<string>();
            foreach (string propertyName in properties)
            {
                var propertyInfo = o.GetType().GetProperty(propertyName);
                var propertyValue = propertyInfo.GetValue(o, null) ?? "";

                recordData.Add(propertyValue.ToString());
            }

            //Todo: replace with method in textProcessing
            return String.Join(",", recordData);
        }

        public static zObject Factory(string zObjectType)
        {
            if (!_constructorDict.Keys.Contains(zObjectType.ToLower()))
            {
                throw new Exception("zObjectType:" + zObjectType + " is not currently supported by composer.");
            }

            return _constructorDict[zObjectType.ToLower()]();
        }

        public static List<string> DescribeZobject(string ztype)
        {
            //Todo: there must be a way to get type without instantiating
            zObject zob = Factory(ztype);

            List<string> props = zob.GetType().GetProperties().Select(prop => prop.Name).ToList();
            Regex matcher = new Regex(@"Specified$");
            props = props.Where(x => !matcher.IsMatch(x)).ToList();

            props.Remove("fieldsToNull");
            return props;
        }

        public static string[] FixPropertyCases(zObject zOb, string[] properties)
        {
            //This method will fix zObject properties with incorrect case, e.g. id should be Id
            List<string> caseCorrectedProperties = new List<string>();
            var validObjectProperties = zOb.GetType().GetProperties();
            Dictionary<string, string> caseLegend = new Dictionary<string, string>();

            foreach (var validObjectProperty in validObjectProperties)
            {
                caseLegend.Add(validObjectProperty.Name.ToLower(), validObjectProperty.Name);
            }

            foreach(string inputProperty in new List<string>(properties)){
                if (caseLegend.Keys.Contains(inputProperty.ToLower()))
                {
                    caseCorrectedProperties.Add(caseLegend[inputProperty.ToLower()]);
                }
                else
                {
                    throw new Exception("Not a valid property:" + inputProperty);
                }
            }

            return caseCorrectedProperties.ToArray();
        }

        public static string FixPropertyCases(string raw, string zObjectType)
        {
            //Todo: replace with a linq function
            List<string> goodProperties = DescribeZobject(zObjectType);

            return goodProperties.FirstOrDefault(goodProp => String.Equals(raw, goodProp, StringComparison.CurrentCultureIgnoreCase));
        }

        public static zObject Compose(Dictionary<string, string> newPropertiesToAdd, string zObjectType, Boolean eraseEmptyFields){

            zObject c = Factory(zObjectType);

            var validObjectProperties = c.GetType().GetProperties();
            List<string> propNames = new List<string>();
            Dictionary<string, Type> propertyTypes = new Dictionary<string, Type>();
            Dictionary<string, string> caseLegend = new Dictionary<string, string>();

            foreach (var validObjectProperty in validObjectProperties)
            {
                propNames.Add(validObjectProperty.Name);
                propertyTypes.Add(validObjectProperty.Name, validObjectProperty.PropertyType);
                caseLegend.Add(validObjectProperty.Name.ToLower(), validObjectProperty.Name);
            }

            foreach (var pair in newPropertiesToAdd)
            {
                //Make sure the object actually has the property we're trying to assign
                if (!caseLegend.Keys.Contains(pair.Key.ToLower()))
                {
                    Console.WriteLine("*** " + pair.Key + " is not a valid field for zobject type " + zObjectType + " ***");
                    Console.Write("Valid fields are:\n" + String.Join(",\n", propNames));
                    throw new Exception();
                }

                string key = caseLegend[pair.Key.ToLower()];

                //Erase empty fields mode means blank dictionary items will overwrite data on the server
                if (String.IsNullOrEmpty(pair.Value) && !eraseEmptyFields)
                {
                    continue;
                }

                //Strings are the simplest case, just write the value with no additional steps.
                if (propertyTypes[key] == typeof(string))
                {
                    c.GetType().GetProperty(key).SetValue(c, pair.Value, null);
                }
                else
                {
                    //Important: non-strings requiring setting the zObject companion property "xSpecified" boolean. 
                    c.GetType().GetProperty(key + "Specified").SetValue(c, true, null);

                    Type t = Nullable.GetUnderlyingType(propertyTypes[key]) ?? propertyTypes[key]; 

                    var value = Convert.ChangeType(pair.Value.ToLower(), t);
                    c.GetType().GetProperty(key).SetValue(c, value, null);
                }
            }

            return c;
        }
    }
}
