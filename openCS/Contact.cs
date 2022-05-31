namespace bardchat
{
    internal sealed record class Contact
    {
        public string name;
        public Guid id;
        
        public Contact(string name, Guid id)
        {
            this.name = name;
            this.id = id;
        }

        public Contact(Guid id)
        {
            this.id = id;
            this.name = id.ToString();
        }
    }
}