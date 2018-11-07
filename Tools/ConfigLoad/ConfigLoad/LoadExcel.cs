﻿using System;
using System.IO;
using Excel;
using System.Data;
using LitJson;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
public struct SelfPropertyInfo
{
    public string defaultData;
    public string fieldType;
    public string propertyName;
    public string generalTarget;
}
namespace ConfigLoad
{
    
    public class LoadExcel
    {
        FileInfo[] fileInfo;
        public JsonData jsRoot;
        public Dictionary<string, string> filePath = new Dictionary<string, string>();
        
      
        public Dictionary<string, List<SelfPropertyInfo>> GeneralCodeData = new Dictionary<string, List<SelfPropertyInfo>>();
        public void LoadGeneralCodeDataFromFile(string path)
        {
            GeneralCodeData.Clear();
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            FileInfo[] fileInfo = dirInfo.GetFiles("*.xlsx", SearchOption.AllDirectories);
            const int keyRow = 0;
            foreach (FileInfo fileData in fileInfo)
            {
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(fileData.OpenRead());
                DataSet excData = excelReader.AsDataSet();
                if (excData == null)
                    continue;
                if (excData.Tables.Count == 0)
                    continue;
                int idStartColumns = 0;
                for (int tabs = 0; tabs < excData.Tables.Count; ++tabs)
                {
                    for (int j = 0; j < excData.Tables[tabs].Columns.Count; ++j)
                    {
                        var key = excData.Tables[tabs].Rows[j][keyRow];
                        if (!(key is string))
                        {
                            idStartColumns = j;
                            break;
                        }
                    }
                }
                List<SelfPropertyInfo> ls_OneClassPropertyInfo = new List<SelfPropertyInfo>();
                for (int tabs = 0; tabs < excData.Tables.Count; ++tabs)
                {
                    for (int i = 1; i < excData.Tables[tabs].Rows.Count; ++i)
                    {
                        if (!(excData.Tables[tabs].Rows[0][i] is string))
                            continue;
                        SelfPropertyInfo spi = new SelfPropertyInfo();
                        Type t = spi.GetType();
                        object obj = Activator.CreateInstance(t);
                        for (int j = 0; j < idStartColumns; ++j)
                        {
                            var key = excData.Tables[tabs].Rows[j][keyRow];
                            if(key is string)
                            {
                                string k = key.ToString();
                                FieldInfo fi = t.GetField(k);
                                if(fi!= null)
                                {
                                    fi.SetValue(obj, excData.Tables[tabs].Rows[j][i].ToString());
                                }
                            }
                            else
                            {
                                Console.WriteLine(key+" is not string");
                            }
                        }
                        ls_OneClassPropertyInfo.Add((SelfPropertyInfo)obj);
                    }
                }

                GeneralCodeData.Add(fileData.Name.Replace(".xlsx", "").Replace(".xls", ""), ls_OneClassPropertyInfo);
            }
        }

        public void OpenFile(string path)
        {
            filePath.Clear();
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            fileInfo = dirInfo.GetFiles("*.xlsx", SearchOption.AllDirectories);

            const int keyRow = 0;

            jsRoot = new JsonData();
            jsRoot.SetJsonType(JsonType.Object);
            JsonData js;

            foreach (FileInfo fileData in fileInfo)
            {
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(fileData.OpenRead());
                DataSet excData = excelReader.AsDataSet();
                if (excData == null)
                    continue;
                if (excData.Tables.Count == 0)
                    continue;

                string fileName = fileData.Name;

                int id = 0;
                JsonData jsData = new JsonData();
                jsData.SetJsonType(JsonType.Object);
                for (int tabs = 0; tabs < excData.Tables.Count; ++tabs)
                {
                    for (int i = 1; i < excData.Tables[tabs].Rows.Count; ++i)
                    {
                        js = new JsonData();
                        js.SetJsonType(JsonType.Object);
                        bool flag = false;
                        for (int j = 0; j < excData.Tables[tabs].Columns.Count; ++j)
                        {
                            string key = excData.Tables[tabs].Rows[keyRow][j].ToString();
                            object data = excData.Tables[tabs].Rows[i][j];

                            if (data is double)
                            {
                                if (data.ToString().Contains("."))
                                {
                                    js[key] = new JsonData((double)data);
                                }
                                else
                                {
                                    js[key] = new JsonData(Convert.ToInt32(data));
                                }
                                flag = true;
                            }
                            else if (data is string)
                            {
                                try
                                {
                                    if ((string)data == " ")
                                        js[key] = new JsonData((string)data);
                                    else
                                    {
                                        JsonReader jsr = new JsonReader((string)data);
                                        JsonData jd = JsonMapper.ToObject(jsr);
                                        js[key] = jd;
                                    }

                                }
                                catch (System.Exception ex)
                                {
                                    js[key] = new JsonData((string)data);
                                }
                                flag = true;
                            }
                        }

                        if (flag)
                            if (js.IsValidKey("id"))
                            {
                                string strid = js["id"].ToString();
                                try
                                {
                                    id = int.Parse(strid);
                                    js["id"] = id;
                                    if (jsData.IsValidKey(strid))
                                        throw new Exception("此ID已存在! ID: " + id.ToString() + " Row: " + i.ToString());
                                    else
                                        jsData[strid] = js;
                                }
                                catch (System.Exception ex)
                                {
                                    MessageBox.Show(ex.Message.ToString() + "/nID: " + strid, fileName);
                                }
                            }
                            else
                            {
                                try
                                {
                                    js["id"] = ++id;
                                    if (jsData.IsValidKey(id.ToString()))
                                        throw new Exception("此ID已存在! ID: " + id.ToString() + " Row: " + i.ToString());
                                    else
                                        jsData[id.ToString()] = js;
                                }
                                catch (System.Exception ex)
                                {
                                    MessageBox.Show(ex.Message.ToString(), fileName);
                                }
                            }
                    }
                }

                string cfgName = fileData.Name.Split('.')[0];
                string cfgPath = "\\" + fileData.Directory.Name + "\\";

                jsRoot[cfgName] = jsData;

                if (!filePath.ContainsKey(cfgName))
                {
                    filePath.Add(cfgName, cfgPath);
                }
            }

            //Console.WriteLine(jsRoot.ToJson());
        }
    }
}
