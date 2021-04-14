using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

public class CreateTable
{
    static void Main()
    {
        DatabaseManager.InitialiseDatabase();        
        DatabaseManager.AddFolderToDatabase(@"D:\Music");
    }
}
