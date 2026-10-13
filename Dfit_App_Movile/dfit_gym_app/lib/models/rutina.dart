import 'package:cloud_firestore/cloud_firestore.dart';

class Rutina {
  final String id;
  final String nombre;
  final String descripcion;
  final String nivel;
  final String videoUrl;
  final bool activa;

  Rutina({
    required this.id,
    required this.nombre,
    required this.descripcion,
    required this.nivel,
    required this.videoUrl,
    required this.activa,
  });

  factory Rutina.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return Rutina(
      id: doc.id,
      nombre: data['Nombre'] ?? '',
      descripcion: data['Descripcion'] ?? '',
      nivel: data['Nivel'] ?? '',
      videoUrl: data['VideoUrl'] ?? '',
      activa: data['Activa'] ?? false,
    );
  }
}
