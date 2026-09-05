using System;
using Google.Cloud.Firestore;


namespace Dfit.Models.Patterns.Singleton
{
    public interface IQRTokenService
    {
        string GetCurrentToken();
        bool ValidateToken(string token);
    }

    [FirestoreData]
    public class QRTokenManager : IQRTokenService
    {
        private string _currentToken;
        private DateTime _tokenExpiration;

        public QRTokenManager()
        {
            GenerateNewToken();
        }

        public string GetCurrentToken()
        {
            if (DateTime.Now > _tokenExpiration)
            {
                GenerateNewToken();
            }
            return _currentToken;
        }

        public bool ValidateToken(string token)
        {
            return token == _currentToken && DateTime.Now <= _tokenExpiration;
        }

        private void GenerateNewToken()
        {
            _currentToken = Guid.NewGuid().ToString();
            _tokenExpiration = DateTime.Now.AddMinutes(1);
        }
    }
}
