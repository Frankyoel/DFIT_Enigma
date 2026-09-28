import 'package:cloud_firestore/cloud_firestore.dart';

class Usuario {
  final String id;
  final String correo;
  final String passwordHash;
  final bool activo;
  final String nombres;
  final String apellidos;
  final String celular;
  final String dni;
  final String rol;

  Usuario({
    required this.id,
    required this.correo,
    required this.passwordHash,
    required this.activo,
    required this.nombres,
    required this.apellidos,
    required this.celular,
    required this.dni,
    required this.rol,
  });

  factory Usuario.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return Usuario(
      id: doc.id,
      correo: data['Correo'] ?? '',
      passwordHash: data['PasswordHash'] ?? '',
      activo: data['Activo'] ?? false,
      nombres: data['Nombres'] ?? '',
      apellidos: data['Apellidos'] ?? '',
      celular: data['Celular'] ?? '',
      dni: data['Dni'] ?? '',
      rol: data['Rol'] ?? 'Usuario',
    );
  }
}
