namespace bardchat
{
    internal sealed class Chat
    {
        public string name;

        public Dictionary<int, string> messages = new Dictionary<int, string>();
        int myId;

        public Dictionary<Contact, Role> members { get; private set;}

        public Chat()
        {
            members = new Dictionary<Contact, Role>();
            name = string.Empty;
        }

        public void AddMember(Contact member) => members.Add(member, Role.Empty);

        public void RemoveMember(Contact member) => members.Remove(member);
    }
}