namespace bardchat
{
    internal sealed class Chat
    {
        public Dictionary<int, string> messages = new Dictionary<int, string>();
        int myId;

        private Dictionary<Contact, Role> members = new Dictionary<Contact, Role>();

        public void AddMember(Contact member) => members.Add(member, Role.Empty);

        public void RemoveMember(Contact member) => members.Remove(member);
    }
}