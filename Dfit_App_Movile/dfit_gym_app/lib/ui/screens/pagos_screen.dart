import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../services/firebase_service.dart';
import '../../main.dart';
import '../../models/pago.dart';

class PagosScreen extends StatelessWidget {
  const PagosScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.read<AuthState>().currentUser!;
    final firebase = context.read<FirebaseService>();

    return Scaffold(
      appBar: AppBar(title: const Text("Historial de Pagos")),
      body: StreamBuilder<List<Pago>>(
        stream: firebase.getPagosStream(user.id),
        builder: (context, snapshot) {
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          final pagos = snapshot.data!;

          if (pagos.isEmpty) {
            return const Center(child: Text("No has realizado pagos recientes."));
          }

          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: pagos.length,
            itemBuilder: (context, index) {
              final p = pagos[index];
              return Card(
                child: ListTile(
                  title: Text(p.membresiaNombre ?? 'Membresía', style: const TextStyle(fontWeight: FontWeight.bold)),
                  subtitle: Text("Fecha: ${p.fechaPago != null ? DateFormat('dd/MM/yyyy HH:mm').format(p.fechaPago!) : 'N/A'}"),
                  trailing: Text(
                    "S/ ${p.monto.toStringAsFixed(2)}",
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.green),
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
