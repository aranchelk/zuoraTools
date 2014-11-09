using System;

namespace zuoraTools.BusinessLogic
{
    class DescribeZuoraObject
    {
        public static void Process(Config conf)
        {
            conf.RequiredProperty("ZObjectType");

            Console.WriteLine("\n" + String.Join("\n", ZObjectHelper.DescribeZobject(conf.ZObjectType)));

        }

        public static void Process()
        {
            Console.WriteLine("\n" + String.Join("\n", ZObjectHelper.SupportedZObjects));
        }
    }
}
