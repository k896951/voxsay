namespace voxsay
{
    public class ProductInfo
    {
        public string Hostname { get; set; }

        public int? Portnumber { get; set; }

        public string Context { get; set; }

        public ProdnameEnum Product { get; set; }

        public ProductInfo(string host, int port, string ctx, ProdnameEnum prod)
        {
            Hostname = host;
            Portnumber = port;
            Context = ctx;
            Product = prod;
        }
    }
}
