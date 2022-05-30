namespace bardchat
{
    internal sealed class Role
    {
        // permissions
        public static Role Empty = new Role(false, false, false, false, string.Empty);

        public bool isAdmin { get; private set;}
        public bool canMute { get; private set;}
        public bool canDeleteMessages { get; private set;}
        public bool canKick { get; private set;}

        public string name { get; private set;}

        public Role(bool isAdmin, bool canMute, bool canDeleteMessages, bool canKick, string name)
        {
            this.isAdmin = isAdmin;
            this.canMute = canMute;
            this.canDeleteMessages = canDeleteMessages;
            this.canKick = canKick;

            this.name = name;
        }
    }
}