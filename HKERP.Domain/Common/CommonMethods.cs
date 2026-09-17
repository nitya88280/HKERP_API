using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace HKERP.Domain.Common
{
    public static class CommonMethods
    {
        public static List<Dictionary<string, object>> ConvertDataTableToList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col];
                    dict[col.ColumnName] = value == DBNull.Value ? null : value;
                }
                list.Add(dict);
            }
            return list;
        }

        public static List<List<Dictionary<string, object>>> ConvertDataSetToList(DataSet ds)
        {
            var result = new List<List<Dictionary<string, object>>>();
            foreach (DataTable table in ds.Tables)
            {
                result.Add(ConvertDataTableToList(table));
            }
            return result;
        }

        public static string Encrypt(string toEncrypt, bool useHashing, string securityKey)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

            if (useHashing)
            {
                using (MD5 hashmd5 = MD5.Create())
                {
                    keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(securityKey));
                }
            }
            else
            {
                keyArray = Encoding.UTF8.GetBytes(securityKey);
            }

            using (TripleDES tdes = TripleDES.Create())
            {
                tdes.Key = keyArray;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform cTransform = tdes.CreateEncryptor())
                {
                    byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                    return Convert.ToBase64String(resultArray, 0, resultArray.Length);
                }
            }
        }

        public static string Decrypt(string cipherString, bool useHashing, string securityKey)
        {
            byte[] keyArray;
            byte[] toDecryptArray = Convert.FromBase64String(cipherString);

            if (useHashing)
            {
                using (MD5 hashmd5 = MD5.Create())
                {
                    keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(securityKey));
                }
            }
            else
            {
                keyArray = Encoding.UTF8.GetBytes(securityKey);
            }

            using (TripleDES tdes = TripleDES.Create())
            {
                tdes.Key = keyArray;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform cTransform = tdes.CreateDecryptor())
                {
                    byte[] resultArray = cTransform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);
                    return Encoding.UTF8.GetString(resultArray);
                }
            }
        }
    }
}
