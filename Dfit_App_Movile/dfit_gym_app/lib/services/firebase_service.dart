import 'package:cloud_firestore/cloud_firestore.dart';
import '../models/usuario.dart';
import '../models/rutina.dart';
import '../models/horario.dart';
import '../models/pago.dart';
import '../models/membresia.dart';

class FirebaseService {
  final FirebaseFirestore _db = FirebaseFirestore.instance;

  // Login: Search in 'Usuarios' collection
  Future<Usuario?> login(String correo, String password) async {
    try {
      var snapshot = await _db.collection('Usuarios')
          .where('Correo', isEqualTo: correo)
          .where('PasswordHash', isEqualTo: password)
          .get();

      if (snapshot.docs.isNotEmpty) {
        var user = Usuario.fromFirestore(snapshot.docs.first);
        if (user.activo) return user;
      }
      return null;
    } catch (e) {
      print('Login error: $e');
      return null;
    }
  }

  // Get User's Active Membership
  Stream<MembresiaUsuario?> getMembresiaActivaStream(String usuarioId) {
    return _db.collection('MembresiasUsuarios')
        .where('UsuarioId', isEqualTo: usuarioId)
        .where('Activa', isEqualTo: true)
        .snapshots()
        .asyncMap((snapshot) async {
          if (snapshot.docs.isEmpty) return null;
          var mu = MembresiaUsuario.fromFirestore(snapshot.docs.first);

          // Join with Membresia details
          var mDoc = await _db.collection('Membresias').doc(mu.membresiaId).get();
          if (mDoc.exists) {
            mu.membresia = Membresia.fromFirestore(mDoc);
          }
          return mu;
        });
  }

  // Get Routines
  Stream<List<Rutina>> getRutinasStream() {
    return _db.collection('Rutinas')
        .where('Activa', isEqualTo: true)
        .snapshots()
        .map((snapshot) => snapshot.docs.map((doc) => Rutina.fromFirestore(doc)).toList());
  }

  // Get Schedules
  Stream<List<Horario>> getHorariosStream() {
    return _db.collection('Horarios')
        .snapshots()
        .map((snapshot) => snapshot.docs.map((doc) => Horario.fromFirestore(doc)).toList());
  }

  // Get Payments with Membership Names
  Stream<List<Pago>> getPagosStream(String usuarioId) {
    return _db.collection('Pagos')
        .where('UsuarioId', isEqualTo: usuarioId)
        .snapshots()
        .asyncMap((snapshot) async {
          List<Pago> pagos = [];
          for (var doc in snapshot.docs) {
            var pago = Pago.fromFirestore(doc);
            var mDoc = await _db.collection('Membresias').doc(pago.membresiaId).get();
            pago.membresiaNombre = mDoc.exists ? mDoc.get('Nombre') : 'N/A';
            pagos.add(pago);
          }
          return pagos;
        });
  }

  // Register Attendance (QR)
  Future<bool> registrarAsistencia(String usuarioId, String qrToken) async {
    try {
      // Logic: App original validates active membership before adding attendance
      var muSnapshot = await _db.collection('MembresiasUsuarios')
          .where('UsuarioId', isEqualTo: usuarioId)
          .where('Activa', isEqualTo: true)
          .get();

      if (muSnapshot.docs.isEmpty) return false;

      await _db.collection('Asistencias').add({
        'UsuarioId': usuarioId,
        'FechaHoraEntrada': FieldValue.serverTimestamp(),
        'TokenUtilizado': qrToken,
      });
      return true;
    } catch (e) {
      print('Attendance error: $e');
      return false;
    }
  }

  // Get Attendance History for Calendar
  Stream<List<DateTime>> getAsistenciasStream(String usuarioId) {
    return _db.collection('Asistencias')
        .where('UsuarioId', isEqualTo: usuarioId)
        .snapshots()
        .map((snapshot) => snapshot.docs
            .map((doc) => (doc.get('FechaHoraEntrada') as Timestamp).toDate())
            .toList());
  }
}
