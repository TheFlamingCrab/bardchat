using System.Text;
using Org.BouncyCastle.Crypto.Digests;

//TODO implement BRHasher class in BRC2 class

internal static class BRHasher
{
    public static Random random = new Random();

    public static string HashText(string text, string salt)
    {
        Sha3Digest hashAlgorithm = new Sha3Digest(512);
        byte[] input = Encoding.ASCII.GetBytes((text + "BARDCODE" + salt));

        hashAlgorithm.BlockUpdate(input, 0, input.Length);

        byte[] result = new byte[64];

        hashAlgorithm.DoFinal(result, 0);

        string hashString = Convert.ToBase64String(result);

        return hashString;
    }
}
