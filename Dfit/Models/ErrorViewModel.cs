using Google.Cloud.Firestore;
namespace Dfit.Models
{
    [FirestoreData]
    public class ErrorViewModel
    {
        [FirestoreProperty]
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
