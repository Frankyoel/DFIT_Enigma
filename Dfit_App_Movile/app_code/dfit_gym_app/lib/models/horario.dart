import 'package:cloud_firestore/cloud_firestore.dart';

class Horario {
  final String id;
  final String nombreClase;
  final String diaSemana;
  final String horaInicio;
  final String horaFin;
  final String instructor;
  final int cupoMaximo;

  Horario({
    required this.id,
    required this.nombreClase,
    required this.diaSemana,
    required this.horaInicio,
    required this.horaFin,
    required this.instructor,
    required this.cupoMaximo,
  });

  factory Horario.fromFirestore(DocumentSnapshot doc) {
    Map data = doc.data() as Map<String, dynamic>;
    return Horario(
      id: doc.id,
      nombreClase: data['NombreClase'] ?? '',
      diaSemana: data['DiaSemana'] ?? '',
      horaInicio: data['HoraInicio'] ?? '',
      horaFin: data['HoraFin'] ?? '',
      instructor: data['Instructor'] ?? '',
      cupoMaximo: data['CupoMaximo'] ?? 0,
    );
  }
}
