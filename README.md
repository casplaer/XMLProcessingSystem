# XMLProcessingSystem

This project is a distributed system for processing XML files containing instrument status data. It consists of two .NET 8 services (`fileparser` and `dataprocessor`) that communicate via RabbitMQ and store data in a SQLite database. The fileparser service reads XML files, validates them, and sends messages to a RabbitMQ queue, while the dataprocessor service consumes these messages and saves the data to a SQLite database.

## 📦 Prerequisites

### To run this project, ensure you have the following installed:

- Docker and Docker Compose

- (Optional) .NET SDK 8.0 for local development or migration updates

- Git for cloning the repository

## 🚀 Setup and Installation

Follow these steps to get the system up and running:

### 1. Clone the Repository

```
git clone https://github.com/casplaer/XMLProcessingSystem
```

### 2. Open the root directory in Terminal or VS Code.

```
cd XMLProcessingSystem\XMLProcessingSystem
```

### 3. Insert Test XML Files

The repository includes test XML file (`status.xml`) in FileParserService/input. Copy it to the input directory if needed.

#### TO INCLUDE YOUR OWN TEST FILES PUT THEM INTO INPUT DIRECTORY. DEFAULT INPUT DIRECTORY IS `FileParserService/input`.

Optionaly, if you have `dotnet` installed you can use file managing cli:
```
cd FileManagerCli
dotnet run
```

### 4. Configure Environment Variables
Create a `.env` file in the root of the project to configure RabbitMQ and database settings. See the section below for the `.env` file structure.

### ⚙️ Environment Variables (`.env` File)
The project uses a `.env` file to configure RabbitMQ and database paths. Create a `.env` file in the root directory (same dir as `docker-compose` file).


### Example variables for `.env`:

```
RABBITMQ_HOST=lab-rabbit
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_NAME=modules
RABBITMQ_AUTOMATIC_RECOVERY_ENABLED=true
RABBITMQ_TOPOLOGY_RECOVERY_ENABLED=true
RABBITMQ_NETWORK_RECOVERY_INTERVAL=5

FILEPARSER_INPUT_DIR=./FileParserService/input
DATAPROCESSOR_DB_DIR=./DataProcessorService/data

DEFAULT_CONNECTION=Data Source=/app/data/modules.db
```

### 5. Build and Run with Docker Compose

Build and start all services (`rabbitmq`, `fileparser`, `dataprocessor`):
```
docker-compose build --no-cache
docker-compose up -d
```

## 🗄️ SQLite Database Initialization
The `dataprocessor` service uses a SQLite database (`modules.db`) to store processed data. The database is automatically created and initialized with Entity Framework Core migrations when the service starts.

### Database Location

Path: `/app/data/modules.db` inside the dataprocessor container.

Host Mapping: Maps to `./DataProcessorService/data/modules.db` on the host (configurable via `DATAPROCESSOR_DB_DIR` in `.env`).
