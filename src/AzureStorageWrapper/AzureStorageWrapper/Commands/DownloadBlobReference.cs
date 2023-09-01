﻿namespace AzureStorageWrapper.Models
{
    public class DownloadBlobReference
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public string Container { get; set; }
        public int ExpiresIn { get; set; }
    }
}