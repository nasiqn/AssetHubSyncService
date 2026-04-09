# AssetHubSyncService
An event-driven integration service that consumes FieldOps events from Azure Service Bus and synchronises assets into AssetHub with full resilience, idempotency, and observability.
AssetHub Service
📌 Overview

This project runs an AssetHub service integrated with the Azure Service Bus emulator using Docker.

🚧 TODO
 Unit tests to be implemented
 Azure Key Vault integration to be done
 Deployment template (Bicep) to be created
🧰 Prerequisites

Before running the project locally, ensure you have:

Docker Desktop installed and running

▶️ Running the Project Locally
Build and start all services using Docker Compose:
docker compose up --build

🧪 Notes
The project uses the Azure Service Bus Emulator for local development
Ensure Docker has sufficient resources (CPU/Memory) allocated
If you make changes to the Service Bus configuration, restart containers:
docker compose down
docker compose up -d

📬 Service Bus Configuration
Topic: fieldops-events
Subscription: assethub-sync

🐳 Services
Azure SQL Edge – backing store for emulator
Service Bus Emulator – local messaging
AssetHub Service – main application
