import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:provider/provider.dart';
import 'services/firebase_service.dart';
import 'models/usuario.dart';
import 'ui/screens/login_screen.dart';
import 'ui/screens/dashboard_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Firebase with your credentials
  await Firebase.initializeApp(
    options: const FirebaseOptions(
      apiKey: 'AIzaSyCRWnAJbsM5QqDwt6Rp8WicffqxPmoYGEE',
      appId: '1:867453974083:android:a183a7eaeaceac2ad5c6f6',
      messagingSenderId: '867453974083',
      projectId: 'dfit-gym-cd550',
      storageBucket: 'dfit-gym-cd550.firebasestorage.app',
    ),
  );

  runApp(
    MultiProvider(
      providers: [
        Provider<FirebaseService>(create: (_) => FirebaseService()),
        ChangeNotifierProvider<AuthState>(create: (_) => AuthState()),
      ],
      child: const MyApp(),
    ),
  );
}

class AuthState with ChangeNotifier {
  Usuario? _currentUser;
  Usuario? get currentUser => _currentUser;

  void login(Usuario user) {
    _currentUser = user;
    notifyListeners();
  }

  void logout() {
    _currentUser = null;
    notifyListeners();
  }
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DFIT Power App',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF1E88E5),
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      debugShowCheckedModeBanner: false,
      home: Consumer<AuthState>(
        builder: (context, auth, _) {
          return auth.currentUser == null
              ? const LoginScreen()
              : const DashboardScreen();
        },
      ),
    );
  }
}
