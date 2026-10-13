import 'package:cloud_firestore/cloud_firestore.dart';

class Pago {
  final String id;
  final String usuarioId;
  final String membresiaId;
  final double monto;
  final DateTime? fechaPago;
  String? membresiaNombre; // Populated in repository

  Pago({
    required this.id,
    required this.usuarioId,
    required this.membresiaId,
    required this.monto,
    this.fechaPago,
    this.membresiaNombre,
  });

  factory Pago.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return Pago(
      id: doc.id,
      usuarioId: data['UsuarioId'] ?? '',
      membresiaId: data['MembresiaId'] ?? '',
      monto: (data['Monto'] ?? 0).toDouble(),
      fechaPago: (data['FechaPago'] as Timestamp?)?.toDate(),
    );
  }
}
