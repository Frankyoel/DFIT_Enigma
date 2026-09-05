using System;
using Google.Cloud.Firestore;

using System.Collections.Generic;
using System.Diagnostics;

namespace Dfit.Models.Patterns.Observer
{
    public interface IObserver
    {
        void Update(Usuario nuevoUsuario);
    }

    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        void Notify(Usuario nuevoUsuario);
    }

    [FirestoreData]
    public class UserRegistrationSubject : ISubject
    {
        private readonly List<IObserver> _observers = new List<IObserver>();

        public void Attach(IObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify(Usuario nuevoUsuario)
        {
            foreach (var observer in _observers)
            {
                observer.Update(nuevoUsuario);
            }
        }
    }

    [FirestoreData]
    public class EmailNotificationObserver : IObserver
    {
        public void Update(Usuario nuevoUsuario)
        {
            Debug.WriteLine($"[OBSERVER] 📧 Enviando correo de bienvenida simulado a: {nuevoUsuario.Correo}");
        }
    }
    
    [FirestoreData]
    public class LogNotificationObserver : IObserver
    {
        public void Update(Usuario nuevoUsuario)
        {
            Debug.WriteLine($"[OBSERVER] 📝 LOG: Nuevo usuario registrado: {nuevoUsuario.Nombres} a las {DateTime.Now}");
        }
    }
}
