using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SierraSoftworks.PackageServer.API
{
    /// <summary>
    /// Authenticates RESTSharp requests using the provided public and private keys
    /// </summary>
    public sealed class Authenticator : RestSharp.IAuthenticator
    {
        /// <summary>
        /// Authenticates RESTSharp requests using the provided <paramref name="publicKey"/>
        /// and <paramref name="privateKey"/>
        /// </summary>
        public Authenticator(string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        /// <summary>
        /// The public key to use during authentication, should be 32 characters long.
        /// </summary>
        public string PublicKey
        { get; set; }

        /// <summary>
        /// The private key to use during authentication, should be 64 characters long.
        /// </summary>
        public string PrivateKey
        { get; set; }

        /// <summary>
        /// Implementation of the authentication logic used by our API server
        /// </summary>
        public void Authenticate(RestSharp.IRestClient client, RestSharp.IRestRequest request)
        {
            var timestamp = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();

            request.AddHeader("X-API-Key", PublicKey);
            request.AddHeader("X-API-Timestamp", timestamp);

            //Now we need to generate the hash of the body
            using (var sha512 = SHA512Managed.Create())
            {
                //Get our actual request path (without the host)
                sha512.TransformBlock(client.BuildUri(request).PathAndQuery);

                //Add our timestamp
                sha512.TransformBlock(timestamp);

                //Add the private key
                sha512.TransformBlock(PrivateKey);

                //Add the body content
                var body = request.Parameters.FirstOrDefault(x => x.Type == RestSharp.ParameterType.RequestBody);
                if (body != null)
                    sha512.TransformBlock(body.Value.ToString());

                var hash = sha512.TransformFinalBlock().ToHexString();
                request.AddHeader("X-API-Hash", hash);
            }
        }
    }

    static class CryptographyExtensions
    {
        public static void TransformBlock(this HashAlgorithm hashAlgorithm, byte[] block)
        {
            var temp = new byte[block.Length];
            hashAlgorithm.TransformBlock(block, 0, block.Length, temp, 0);
        }

        public static void TransformBlock(this HashAlgorithm hashAlgorithm, string content, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var bytes = encoding.GetBytes(content);
            hashAlgorithm.TransformBlock(bytes);
        }

        public static byte[] TransformFinalBlock(this HashAlgorithm hashAlgorithm)
        {
            return hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
        }

        /// <summary>
        /// Converts a byte array into its hexadecimal string representation
        /// </summary>
        /// <param name="data">The byte array to convert</param>
        /// <returns>Returns a lowercase string representation of the <paramref name="data"/> array in hex form</returns>
        public static string ToHexString(this byte[] data)
        {
            return data.Select(x => x.ToString("x")).Aggregate((x, y) => x + y);
        }
    }
}
