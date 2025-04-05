using System;

namespace Consumer.Models
{
    public class VideoMetadata
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VideoUpload
    {
        public Guid Id { get; set; }
        public VideoMetadata Metadata { get; set; }
        public byte[] VideoData { get; set; }
        public string StoragePath { get; set; }
        public DateTime UploadTime { get; set; }
        public bool IsProcessed { get; set; }

        public VideoUpload()
        {
            Id = Guid.NewGuid();
            UploadTime = DateTime.UtcNow;
            IsProcessed = false;
        }
    }
}
