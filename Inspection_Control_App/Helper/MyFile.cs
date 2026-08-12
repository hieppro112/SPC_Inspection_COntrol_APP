using Inspection_Control_App.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inspection_Control_App.Helper
{
    public class MyFile
    {
        public Inspection_config ReadJson(string path = @"C:\Inspection_Config\config_Inspection.json")
        {

            if (File.Exists(path))
            {
                string jsonW = File.ReadAllText(path);
                Inspection_config config = JsonSerializer.Deserialize<Inspection_config>(jsonW);
                return new Inspection_config
                {
                    name_inspection = config.name_inspection
                };
            }
            else
            {
                Inspection_config item = new Inspection_config();
                string json = JsonSerializer.Serialize(item);
                Directory.CreateDirectory(@"C:/Inspection_Config/");
                File.WriteAllText(@"C:\Inspection_Config\config_Inspection.json", json);
                return null;
            }
        }
    }
}
