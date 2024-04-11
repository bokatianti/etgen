
using etgen.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Xml;

namespace etgen
{
    class Program
    {
        string title = "N/A", ulesdatum = "N/A", referens = "N/A", prev = "N/A", content = "", csvpath = "N/A", exec = "N/A", javaslat = "N/A", etdatum = "N/A", tipus = "N/A", melleklet = "N/A", outputfile="N/A";
        public struct macros
        {
            public string macroName, macroContent;
        };
        List<macros> loadedMacros = new List<macros>();
        int[] eredmeny = { 0, 0, 0, 0 };
        static void Main(string[] args)
        {
            foreach (string arg in args)
                Console.WriteLine(arg);
            if (args.Length > 0 && args[0] == "clean")
            {
                if (Directory.Exists(Directory.GetCurrentDirectory() + "\\eloterjesztes_res"))
                    Directory.Delete(Directory.GetCurrentDirectory() + "\\eloterjesztes_res", true);
            }              
            else if (args.Length == 3 && args[0] == "build" && args[1].Contains(".ets") && args[2].Contains(".tex"))
            {
                try
                {
                    args[1] = args[1].Replace("\"", "");
                    args[2] = args[2].Replace("\"", "");
                    Program pr = new Program();
                    pr.createResource("bme_logo_nagy.jpg", Path.GetDirectoryName(args[2]));
                    pr.createResource("hklogo.png", Path.GetDirectoryName(args[2]));
                    pr.loadEts(args[1]);
                    pr.Write(args[2]);
                }catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                }
                
            }
            else if (args.Length == 2 && args[0] == "build" && args[1].Contains(".ets"))
            {
                try
                {
                    args[1] = args[1].Replace("\"", "");
                    Program pr = new Program();
                    pr.loadEts(args[1]);
                    pr.Write("ETS_SOURCE");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                }

            }
            else if(args.Length == 1 && args[0].Contains(".ets"))
            {
                try
                {
                    args[0] = args[0].Replace("\"", "");
                    string directoryPath = Path.GetDirectoryName(args[0]);
                    string filePath = directoryPath + "\\build.bat";
                    Program pr = new Program();
                    string out2delete = pr.loadEts(args[0]);
                    pr.Write("ETS_SOURCE");
                    StreamWriter batch =  new StreamWriter(filePath);
                    batch.WriteLine("cd /D " + directoryPath);
                    batch.WriteLine("latexmk -pdf");
                    batch.WriteLine("latexmk -c");
                    batch.Close();
                    var process = Process.Start(filePath);
                    process.WaitForExit();
                    File.Delete(filePath);
                    if (Directory.Exists(directoryPath + "\\eloterjesztes_res"))
                        Directory.Delete(directoryPath + "\\eloterjesztes_res", true);
                    File.Delete(out2delete);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                }
            }    
            else
            {
                Console.WriteLine("Incorrect arguments, press any key to exit");
                Console.ReadLine();
            }        
        }

        void createResource(string resFileName, string outputFolder)
        {
            string resourceName = "etgen.Resources." + resFileName;
            string resFolder = outputFolder + "\\eloterjesztes_res";
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (!Directory.Exists(resFolder))
                Directory.CreateDirectory(resFolder);
            var fileStream = File.Create(resFolder+"\\"+resFileName);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(fileStream);
            fileStream.Close();
        }
        string loadEts(string filename) 
        {
            if (File.Exists(filename))
            {
                bool iscontent = false, ismacros = false, isjav = false, ismell = false;
                foreach(var line  in File.ReadAllLines(filename)) 
                {                    
                    if (line.Length > 3)
                    {
                        string c = line.Substring(0, 4);
                        switch (c)
                        {
                            case ("outf"): outputfile = cfgRead(line); break;
                            case ("cime"): title = cfgRead(line); break;
                            case ("ules"): ulesdatum = cfgRead(line); break;
                            case ("rnev"): referens = cfgRead(line); break;
                            case ("prev"): prev = cfgRead(line); break;
                            case ("csvl"): csvpath = cfgRead(line);
                                if (csvpath == "DEFAULT") csvpath = Directory.GetCurrentDirectory() + "\\export.csv";
                                break;
                            case ("vegr"): exec = cfgRead(line); break;
                            case ("javs"): javaslat = cfgRead(line); break;
                            case ("date"): etdatum = cfgRead(line); break;
                            case ("mell"): melleklet = cfgRead(line); break;
                            case ("type"): tipus = cfgRead(line); break;
                            case ("vres"):
                                string results = cfgRead(line);
                                for (int i = 0; i < 4; i++)
                                    eredmeny[i] = Int32.Parse(results.Split(';')[i]);
                                break;
                            default: if (iscontent && !line.Contains("$ENDCONTENT$")) content += rMacro(line) + "\n"; break;
                        }
                    }
                    if (iscontent)
                    {
                        if (line.Contains("$ENDCONTENT$"))
                            iscontent = false;
                    } 
                    else if (ismacros)
                    {
                        if (line.Contains("$ENDMACROS$"))
                            ismacros = false;
                        else if (line.Contains("="))
                        {
                            macros newMacro;
                            newMacro.macroName = line.Split("=")[0];
                            newMacro.macroContent = line.Split("=")[1];
                            loadedMacros.Add(newMacro);
                        }    
                    }
                    if (line.Contains("$CONTENT$"))
                        iscontent = true;
                    if (line.Contains("$MACROS$"))
                        ismacros = true;              
                }
            }
            return outputfile;
        }
        string rMacro(string line)
        {
            foreach (var macro in loadedMacros)
            {
                line = line.Replace(macro.macroName, macro.macroContent);
            }
            return line;
        }
        string cfgRead(string line)
        {
            return rMacro(line.Split('$')[1]);
        }
        void Write(string filename)
        {
            if (filename == "ETS_SOURCE")
            {
                filename = outputfile;
                createResource("bme_logo_nagy.jpg", Path.GetDirectoryName(outputfile));
                createResource("hklogo.png", Path.GetDirectoryName(outputfile));
            }
                
            List<string> form = new List<string>();

            using (StringReader reader = new StringReader(global::etgen.Properties.Resources.eloterjesztes_form))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string final = line;
                    final = final.Replace("ULESDATUM", ulesdatum);
                    string ulestipus;
                    switch (tipus)
                    {
                        case ("ELEKTRONIKUS"): ulestipus = "elektronikus szavazására"; break;
                        case ("RENDKIVULI"): ulestipus = "rendkívüli ülésére"; break;
                        default: ulestipus = "ülésére"; break;
                    }
                    final = final.Replace("ULESTIPUS", ulestipus);
                    form.Add(final);
                }
                
            }
            StreamWriter output = new StreamWriter(filename);
            for (int i = 0; i < form.Count; i++)
            {
                if (form[i].Contains("$CONTENTSPACE$"))
                {
                    i++;
                    output.WriteLine("\\textbf{Előterjesztés címe:}\\par");
                    output.WriteLine(title + @"\\");
                    output.WriteLine("\\textbf{Előterjesztő:}\\par");
                    output.WriteLine(referens + @"\\");
                    if(prev!="N/A")
                    {
                        output.WriteLine("\\textbf{Korábbi határozatok, előterjesztések:}\\par");
                        output.WriteLine(prev + @"\\");
                    }
                    output.WriteLine("\\textbf{Az előterjesztés tartalma:}\\par");
                    content.Replace("\n", "\\");
                    output.WriteLine(content);
                    if(csvpath != "N/A")
                    {
                        output.WriteLine(@"\begin{center}");                       
                        bool header = true;
                        foreach (var line in File.ReadAllLines(csvpath))
                        {
                            if(header)
                            {
                                string lineend = @"\begin{longtable}{|";
                                for (int j = 0; j < line.Split(';').Length; j++)
                                    lineend += "c|";
                                lineend += "}";
                                output.WriteLine(lineend);
                                output.WriteLine(@"\hline");
                                header = false;
                            }
                            string[] comp = line.Split(';');
                            string outString = comp[0];
                            for(int j = 1; j < comp.Length; j++) 
                            {
                                outString += "&" + comp[j];
                            }
                            outString += @" \\ \hline";
                            output.WriteLine(outString);                           
                        }
                        output.WriteLine(@"\end{longtable}");
                        output.WriteLine(@"\end{center}");
                    }
                    output.WriteLine("\\textbf{Határozati javaslat:}\\par");
                    output.WriteLine(javaslat + @"\\");
                    output.WriteLine("\\textbf{Végrehajtásért felelősök:}\\par");
                    output.WriteLine(exec + @"\\");
                    if(melleklet != "N/A")
                    {
                        output.WriteLine("\\textbf{Mellékletek:}\\par");
                        output.WriteLine(melleklet + @"\\");
                    }
                    output.WriteLine("\\textbf{Dátum:} " + etdatum + "\\par");
                    string arany =eredmeny[0].ToString();
                    for(int k=1; k<4; k++)
                    {
                        arany += "-" + eredmeny[k].ToString();
                    }
                    string ulestipus;
                    switch(tipus)
                    {
                        case ("ELEKTRONIKUS"): ulestipus = "elektronikus szavazásán "; break;
                        case ("RENDKIVULI"): ulestipus = "rendkívüli ülésén "; break;
                        default: ulestipus = "ülésén "; break;
                    }
                    output.WriteLine("A Hallgatói Képviselet a " + ulesdatum + " napon tartott "+ ulestipus + arany + " arányban támogatta az előterjesztést.");
                }
                output.WriteLine(form[i]);
            }
            output.Close();
        }
    }
}