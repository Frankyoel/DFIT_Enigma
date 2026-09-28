import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../services/firebase_service.dart';
import '../../models/horario.dart';

class HorariosScreen extends StatelessWidget {
  const HorariosScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final firebase = context.read<FirebaseService>();

    return Scaffold(
      appBar: AppBar(title: const Text("Horarios y Clases")),
      body: StreamBuilder<List<Horario>>(
        stream: firebase.getHorariosStream(),
        builder: (context, snapshot) {
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          final horarios = snapshot.data!;

          if (horarios.isEmpty) {
            return const Center(child: Text("No hay clases programadas."));
          }

          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: horarios.length,
            itemBuilder: (context, index) {
              final h = horarios[index];
              return Card(
                child: ListTile(
                  title: Text(h.nombreClase, style: const TextStyle(fontWeight: FontWeight.bold)),
                  subtitle: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text("${h.diaSemana} | ${h.horaInicio} - ${h.horaFin}"),
                      Text("Instructor: ${h.instructor}"),
                      Text("Cupos Máximos: ${h.cupoMaximo}"),
                    ],
                  ),
                  isThreeLine: true,
                ),
              );
            },
          );
        },
      ),
    );
  }
}
