import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../services/firebase_service.dart';
import '../../models/rutina.dart';

class RutinasScreen extends StatelessWidget {
  const RutinasScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final firebase = context.read<FirebaseService>();

    return Scaffold(
      appBar: AppBar(title: const Text("Tus Rutinas")),
      body: StreamBuilder<List<Rutina>>(
        stream: firebase.getRutinasStream(),
        builder: (context, snapshot) {
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          final rutinas = snapshot.data!;

          if (rutinas.isEmpty) {
            return const Center(child: Text("No hay rutinas disponibles en este momento."));
          }

          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: rutinas.length,
            itemBuilder: (context, index) {
              final r = rutinas[index];
              return Card(
                child: ListTile(
                  title: Text(r.nombre, style: const TextStyle(fontWeight: FontWeight.bold)),
                  subtitle: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text("Nivel: ${r.nivel}"),
                      Text(r.descripcion),
                    ],
                  ),
                  trailing: IconButton(
                    icon: const Icon(Icons.play_circle, color: Colors.red),
                    onPressed: () async {
                      if (await canLaunchUrl(Uri.parse(r.videoUrl))) {
                        await launchUrl(Uri.parse(r.videoUrl));
                      }
                    },
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
