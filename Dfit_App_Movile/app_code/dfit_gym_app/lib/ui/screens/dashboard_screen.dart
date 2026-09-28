import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../services/firebase_service.dart';
import '../../main.dart';
import '../../models/membresia.dart';
import 'rutinas_screen.dart';
import 'horarios_screen.dart';
import 'pagos_screen.dart';
import 'qr_scanner_screen.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthState>().currentUser!;
    final firebase = context.read<FirebaseService>();

    return Scaffold(
      appBar: AppBar(
        title: const Text("DFIT Power"),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => context.read<AuthState>().logout(),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              "Hola, ${user.nombres}",
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 24),

            // Membership Card
            StreamBuilder<MembresiaUsuario?>(
              stream: firebase.getMembresiaActivaStream(user.id),
              builder: (context, snapshot) {
                final mu = snapshot.data;
                return Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text("Tu Membresía", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                        const Divider(),
                        if (mu != null) ...[
                          Text("Plan: ${mu.membresia?.nombre ?? 'Cargando...'}"),
                          Text("Vence el: ${mu.fechaFin != null ? DateFormat('dd/MM/yyyy').format(mu.fechaFin!) : 'N/A'}"),
                          const Chip(label: Text("Activa"), backgroundColor: Colors.green),
                        ] else ...[
                          const Text("No tienes una membresía activa"),
                        ],
                      ],
                    ),
                  ),
                );
              },
            ),

            const SizedBox(height: 24),
            const Text("Acciones Rápidas", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: 2,
              mainAxisSpacing: 16,
              crossAxisSpacing: 16,
              children: [
                _DashboardButton(
                  title: "Rutinas",
                  icon: Icons.fitness_center,
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const RutinasScreen())),
                ),
                _DashboardButton(
                  title: "Horarios",
                  icon: Icons.calendar_month,
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const HorariosScreen())),
                ),
                _DashboardButton(
                  title: "Check-In (QR)",
                  icon: Icons.qr_code_scanner,
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const QRScannerScreen())),
                ),
                _DashboardButton(
                  title: "Pagos",
                  icon: Icons.history,
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const PagosScreen())),
                ),
              ],
            ),

            const SizedBox(height: 24),
            const Text("Tu Asistencia", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            // Attendance History (Mini list or Calendar)
            StreamBuilder<List<DateTime>>(
              stream: firebase.getAsistenciasStream(user.id),
              builder: (context, snapshot) {
                if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
                final dates = snapshot.data!;
                return Card(
                  child: ListView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: dates.length > 5 ? 5 : dates.length,
                    itemBuilder: (context, index) {
                      return ListTile(
                        leading: const Icon(Icons.check_circle, color: Colors.green),
                        title: Text(DateFormat('dd MMMM yyyy').format(dates[index])),
                        subtitle: Text(DateFormat('HH:mm').format(dates[index])),
                      );
                    },
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _DashboardButton extends StatelessWidget {
  final String title;
  final IconData icon;
  final VoidCallback onTap;

  const _DashboardButton({required this.title, required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Card(
        color: Colors.blue.withOpacity(0.1),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 40, color: Colors.blue),
            const SizedBox(height: 8),
            Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }
}
