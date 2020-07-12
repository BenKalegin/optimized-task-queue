using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TestHeavyWait
{
    internal class HttpListener
    {
        private static readonly byte[] STestCertBytes = Convert.FromBase64String(@"
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
K8q0Q70zf0r+xMHnewk9APFqUjguPguTdpCoH0VAQST9Mmriv/J12+Y+fL6H+jrt
DY2zHPxTF85pA4bBBnLA7Qt9TKCe6uuWu5yBqxOV3w2Oa4Pockv1gJzFbVnwlEUW
nIjbWVIyo9vo4LBd03uJHPPIQbUp9kCP/Zw+Zblo42/ifyY+a+scwl1q1dZ7Y0L9
2yJCKm9Qf6Q+1PBK+uU9pcuVTg/Imqcg5T7jFO5QCi88uwcorgQp+qoeFi0F9tnU
ecfDl6d0PSgAPnX9XA0ny3bPwSiWOA8+uW73gesxnGTsNrtc1j85tail8N6m6S2t
HXwOmM65J4XRZlzzeM4D/Rzzh13xpRA9kzm9T2cSHsXEYmSW1X7WovrmYhdOh9K3
DPwSyG4tD58cvC7X79UbOB+d17ieo7ZCj+NSLVQO1BqTK0QfErdoVHGKfQG8Lc/E
RQRqj132Mhi2/r5Ca7AWdqD7/3wgRdQTJSFXt/akpM44xu5DMTCISEFOLWiseSOB
tzT6ssaq2Q35dCkXp5wVbWxkXAD7Gm34FFXXyZrJWAx45Y40wj/0KDJoEzXCuS4C
yiskx1EtYNNOtfDC5wngywmINFUnnW0NkdKSxmDJvrT6HkRKN8ftik7tP4ZvTaTS
28Z0fDmWJ+RjvZW+vtF6mrIzYgGOgdpZwG0ZOSKrXKrY3xpMO16fXyawFfBosLzC
ty7uA57niPS76UXdbplgPanIGFyceTg1MsNDsd8vszXd4KezN2VMaxvw+93s0Uk/
3Mc+5MAj+UhXPi5UguXMhNo/CU7erzyxYreOlAI7ZzGhPk+oT9g/MqWa5RpA2IBU
aK/wgaNaHChfCcDj/J1qEl6YQQboixxp1IjQxiV9bRQzgwf31Cu2m/FuHTTkPCdx
DK156pyFdhcgTpTNy7RPLDGB3TATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkrBgEE
AYI3EQExUB5OAE0AaQBjAHIAbwBzAG8AZgB0ACAAUwB0AHIAbwBuAGcAIABDAHIA
eQBwAHQAbwBnAHIAYQBwAGgAaQBjACAAUAByAG8AdgBpAGQAZQByMGcGCSqGSIb3
DQEJFDFaHlgAQwBlAHIAdABSAGUAcQAtADcAOQA4AGUANQA4AGIANQAtAGMAOQA2
ADQALQA0ADcAZQA2AC0AYQAzADIAOQAtADAAMQBjAGEAZABmADcANgAyAGEANgA5
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
ENCljwICB9A=");
        //https://stackoverflow.com/questions/9982865/sslstream-example-how-do-i-get-certificates-that-work
        //openssl req -x509 -config openssl.cnf -newkey rsa:4096 -sha256 -out ssl-cacert.pem -keyout ssl-cakey.pem -outform PEM

        private static readonly Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static X509Certificate2 GetServerCertificate()
        {
            var certCollection = new X509Certificate2Collection();
            certCollection.Import(STestCertBytes, "testcertificate", X509KeyStorageFlags.DefaultKeySet);
            return certCollection.Cast<X509Certificate2>().First();//c => c.HasPrivateKey);
        }

        public Uri CreateSocketServer()
        {
            Listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            Listener.Listen(int.MaxValue);
            var ep = (IPEndPoint)Listener.LocalEndPoint;
            var serverCert = GetServerCertificate();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Socket s = await Listener.AcceptAsync();
                        var unused = Task.Run(async () =>
                        {
                            //using(serverCert)
                            try
                            {
                                using (var serverStream = new SslStream(new NetworkStream(s, true), false,
                                    delegate { return true; }))
                                {
                                    await serverStream.AuthenticateAsServerAsync(serverCert, false, SslProtocols.None,
                                        false);
                                    using (var reader = new StreamReader(serverStream))
                                    using (var writer = new StreamWriter(serverStream) {AutoFlush = true})
                                    {
                                        while (true)
                                        {
                                            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                                            await writer.WriteAsync(
                                                "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello");
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            });
            return new Uri($"https://{ep.Address}:{ep.Port}/");
        }
    }
}
