using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Flusher.Common.Models;
using Newtonsoft.Json;

namespace Flusher.Forms.Helpers
{
    public static class CacheHelper
    {
        public static ObservableCollection<OperationMessage> LoadOperations()
        {
            var data = new ObservableCollection<OperationMessage>();

            try
            {
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OperationsCache.json");

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);

                    var ops = JsonConvert.DeserializeObject<ObservableCollection<OperationMessage>>(json);

                    if (ops != null)
                    {
                        foreach (var operationMessage in ops)
                        {
                            data.Add(operationMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Could not load cached data: {ex.Message}");
            }

            return data;
        }

        public static bool SaveOperations(ObservableCollection<OperationMessage> ops)
        {
            try
            {
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OperationsCache.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var json = JsonConvert.SerializeObject(ops);

                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Delete local file exception: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteLocalImageFile(OperationMessage op)
        {
            try
            {
                var fileName = Path.GetFileName(op.ImageUrl);

                if (string.IsNullOrEmpty(fileName)) return false;

                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);

                if (string.IsNullOrEmpty(filePath)) return false;

                if (!File.Exists(filePath))  return false;

                File.Delete(filePath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Delete local file exception: {ex.Message}");
                return false;
            }
        }
    }
}
