# Gotify Windows Client

Client Windows natif en C# WPF pour recevoir des notifications depuis un serveur Gotify.

## Fonctionnalités

- ✅ Connexion au serveur Gotify via WebSocket
- ✅ Réception des notifications en temps réel
- ✅ Notifications Windows natives (balloon tips)
- ✅ Icône dans la barre système (system tray)
- ✅ Minimisation automatique vers la barre système
- ✅ Liste des messages reçus
- ✅ Sauvegarde automatique de la configuration

## Prérequis

- Windows 10/11
- .NET 6.0 Runtime ou SDK
- Serveur Gotify avec un token client

## Installation

### Option 1 : Compiler depuis les sources

1. Installez [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)

2. Ouvrez un terminal dans le dossier du projet

3. Compilez le projet :
```bash
dotnet build -c Release
```

4. L'exécutable sera dans : `bin/Release/net6.0-windows/GotifyClient.exe`

### Option 2 : Publier une version autonome

Pour créer un exécutable qui n'a pas besoin de .NET installé :

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

L'exécutable sera dans : `bin/Release/net6.0-windows/win-x64/publish/GotifyClient.exe`

## Utilisation

1. Lancez `GotifyClient.exe`

2. Configurez la connexion :
   - **Server URL** : L'URL de votre serveur Gotify (ex: `https://gotify.example.com`)
   - **Client Token** : Votre token d'application client (à créer dans l'interface Gotify)

3. Cliquez sur "Connecter"

4. L'application se connecte et commence à recevoir les notifications

### Obtenir un token client

1. Connectez-vous à votre interface web Gotify
2. Allez dans **Applications**
3. Créez une nouvelle application
4. Copiez le **token** généré
5. Collez-le dans le champ "Client Token" de l'application

## Configuration

La configuration (URL du serveur et token) est sauvegardée automatiquement dans le fichier `gotify_config.json` dans le même dossier que l'exécutable.

## Fonctionnement

- **Minimiser vers la barre système** : Cochez cette option pour que l'application se minimise dans la barre système au lieu de la barre des tâches
- **Double-clic sur l'icône** : Restaure la fenêtre
- **Clic droit sur l'icône** : Affiche le menu contextuel (Ouvrir/Quitter)
- **Effacer les messages** : Supprime l'historique des messages affichés (ne supprime pas les messages du serveur)

## Dépannage

### L'application ne se connecte pas

- Vérifiez que l'URL du serveur est correcte (avec https:// ou http://)
- Vérifiez que le token est valide
- Assurez-vous que le serveur Gotify est accessible depuis votre réseau
- Vérifiez que le port WebSocket est ouvert (généralement le même que HTTP/HTTPS)

### Les notifications n'apparaissent pas

- Vérifiez que les notifications Windows sont activées dans les paramètres système
- Testez en envoyant un message depuis l'interface web Gotify

### Erreur de certificat SSL

Si vous utilisez un certificat auto-signé, vous devrez peut-être modifier le code pour accepter les certificats non valides (non recommandé en production).

## Technologies utilisées

- C# / .NET 6
- WPF (Windows Presentation Foundation)
- WebSocket pour la connexion temps réel
- System.Windows.Forms pour l'icône système

## Licence

MIT License - Libre d'utilisation et modification

## Support

Pour signaler un bug ou demander une fonctionnalité, créez une issue sur le dépôt GitHub.
