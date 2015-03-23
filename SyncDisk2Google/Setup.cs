using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace SyncDisk2Google
{
    public class Setup
    {
        public string GoogleClientJson { get; set; }
        public string LocalPath {get;set;}
        public Setup()
        {
            GoogleClientJson = "./client_secret.json";
            LocalPath = "./save";
        }
        public static Setup FromStream(Stream stream)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Setup));
                return (Setup)xmlSerializer.Deserialize(stream);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
            return new Setup();
            
        }
        public bool ToStream(Stream stream)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Setup));
                xmlSerializer.Serialize(stream, this);
                return true;
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
            return false;
        
        }
    }
}
