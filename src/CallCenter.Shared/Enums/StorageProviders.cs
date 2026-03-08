namespace CallCenter.Shared.Enums;

public static class StorageProviders
{
    public static readonly TypeItem LocalDisk = new(6, "LocalDisk", "StorageProvider.LocalDisk", "Yerel Disk", "bi-hdd-fill", "bg-secondary", 0);
    public static readonly TypeItem GoogleDrive = new(1, "GoogleDrive", "StorageProvider.GoogleDrive", "Google Drive", "bi-google", "bg-danger", 1);
    public static readonly TypeItem OneDrive = new(2, "OneDrive", "StorageProvider.OneDrive", "Microsoft OneDrive", "bi-microsoft", "bg-primary", 2);
    public static readonly TypeItem YandexDisk = new(3, "YandexDisk", "StorageProvider.YandexDisk", "Yandex Disk", "bi-cloud-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem AmazonS3 = new(4, "AmazonS3", "StorageProvider.AmazonS3", "Amazon S3", "bi-cloud-arrow-up-fill", "bg-warning", 4);
    public static readonly TypeItem MinIO = new(5, "MinIO", "StorageProvider.MinIO", "MinIO (S3 uyumlu)", "bi-hdd-rack-fill", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { LocalDisk, GoogleDrive, OneDrive, YandexDisk, AmazonS3, MinIO };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int LocalDisk = 6;
        public const int GoogleDrive = 1;
        public const int OneDrive = 2;
        public const int YandexDisk = 3;
        public const int AmazonS3 = 4;
        public const int MinIO = 5;
    }
}
