import 'package:cloud_firestore/cloud_firestore.dart';

class Membresia {
  final String id;
  final String nombre;
  final String descripcion;
  final int duracionDias;
  final double precio;
  final bool activa;

  Membresia({
    required this.id,
    required this.nombre,
    required this.descripcion,
    required this.duracionDias,
    required this.precio,
    required this.activa,
  });

  factory Membresia.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return Membresia(
      id: doc.id,
      nombre: data['Nombre'] ?? '',
      descripcion: data['Descripcion'] ?? '',
      duracionDias: data['DuracionDias'] ?? 0,
      precio: (data['Precio'] ?? 0).toDouble(),
      activa: data['Activa'] ?? false,
    );
  }
}

class MembresiaUsuario {
  final String id;
  final String usuarioId;
  final String membresiaId;
  final DateTime? fechaInicio;
  final DateTime? fechaFin;
  final bool activa;
  Membresia? membresia; // Join logic handled in repository

  MembresiaUsuario({
    required this.id,
    required this.usuarioId,
    required this.membresiaId,
    this.fechaInicio,
    this.fechaFin,
    required this.activa,
    this.membresia,
  });

  factory MembresiaUsuario.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return MembresiaUsuario(
      id: doc.id,
      usuarioId: data['UsuarioId'] ?? '',
      membresiaId: data['MembresiaId'] ?? '',
      fechaInicio: (data['FechaInicio'] as Timestamp?)?.toDate(),
      fechaFin: (data['FechaFin'] as Timestamp?)?.toDate(),
      activa: data['Activa'] ?? false,
    );
  }
}
