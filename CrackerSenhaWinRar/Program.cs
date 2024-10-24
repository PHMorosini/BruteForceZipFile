using Ionic.Zip;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string zipFilePath = @"D:\Migração\Bancos\Justusz Bebidas\Banco\Bkp.zip";
        string outputFolder = @"D:\Migração\Bancos\Justusz Bebidas";
        int maxPasswordLength = 10; // Teste com 2 caracteres

        BruteForceZipPassword(zipFilePath, outputFolder, maxPasswordLength);
    }

    static void BruteForceZipPassword(string zipFilePath, string outputFolder, int maxPasswordLength)
    {
        string charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/@#";
        var passwords = GeneratePasswords(charSet, maxPasswordLength);
        string foundPassword = null;
        object lockObject = new object(); // Para evitar concorrência na atualização de variáveis compartilhadas

        // Usar paralelismo para testar as senhas
        Parallel.ForEach(passwords, (password, state) =>
        {
            if (foundPassword != null)
                state.Stop(); // Para todas as outras execuções se a senha for encontrada

            Console.WriteLine($"Tentando senha: {password}");

            if (TryExtractWithPassword(zipFilePath, outputFolder, password))
            {
                lock (lockObject)
                {
                    if (foundPassword == null)
                    {
                        foundPassword = password;
                        Console.WriteLine($"Senha encontrada: {foundPassword}");
                        state.Stop(); // Interrompe a execução paralela ao encontrar a senha
                    }
                }
            }
        });

        if (foundPassword != null)
        {
            Console.WriteLine($"Senha encontrada: {foundPassword}");
        }
        else
        {
            Console.WriteLine("Nenhuma senha foi encontrada.");
        }
    }

    static bool TryExtractWithPassword(string zipFilePath, string outputFolder, string password)
    {
        try
        {
            using (ZipFile zip = ZipFile.Read(zipFilePath))
            {
                zip.Password = password; // Define a senha

                // Apenas verificar a senha com uma entrada qualquer, sem extrair o conteúdo completo
                foreach (ZipEntry entry in zip)
                {
                    using (var ms = new MemoryStream())
                    {
                        entry.Extract(ms); // Teste rápido da senha
                    }
                    break; // Tenta apenas o primeiro arquivo
                }
                return true; // Sucesso
            }
        }
        catch (BadPasswordException)
        {
            return false; // Senha incorreta
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return false;
        }
    }

    static IEnumerable<string> GeneratePasswords(string charSet, int maxPasswordLength)
    {
        for (int length = 1; length <= maxPasswordLength; length++)
        {
            foreach (string password in GeneratePasswordsRecursive(charSet, "", length))
            {
                yield return password;
            }
        }
    }

    static IEnumerable<string> GeneratePasswordsRecursive(string charSet, string current, int maxLength)
    {
        if (current.Length == maxLength)
        {
            yield return current;
        }
        else
        {
            foreach (char c in charSet)
            {
                foreach (string password in GeneratePasswordsRecursive(charSet, current + c, maxLength))
                {
                    yield return password;
                }
            }
        }
    }
}
