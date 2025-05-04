# Living-Paris

---

## Code owner
- jojo2504 | Jonathan Tran
  
## Collaborators
- Bahyamin | Benjamin Zilber
- Tibo-7 | Thilbault Thery

## Introduction

Living Paris is an application that facilitates meal sharing between neighbors within Paris. The platform allows individuals or local businesses to order homemade meals prepared by registered cooks. Users can register as either a cook or a client—or both simultaneously—by providing their personal details such as name, address, phone number, and email.

This project aims to explore graphs and their functionality, particularly in optimizing delivery routes and analyzing user interactions.

**This repository is a project for A2/S4 students in computer sciences.**

- [Living-Paris](#living-paris)
  - [Code owner](#code-owner)
  - [Collaborators](#collaborators)
  - [Introduction](#introduction)
  - [Feature highlights](#feature-highlights)
  - [Getting started](#getting-started)
    - [From release](#from-release)
    - [Build from source code](#build-from-source-code)
  - [Development](#development)
    - [Structure](#structure)
    - [Database](#database)
  - [Improvements](#improvements)
    - [Known issues](#known-issues)
    - [To do](#to-do)
  - [User Roles](#user-roles)
    - [👨‍🍳  Chef](#--chef)
    - [🧑‍💼 Client](#-client)
    - [⚙️ Admin](#️-admin)
  - [Advanced Features](#advanced-features)
    - [📊 Relationship Visualization](#-relationship-visualization)
    - [🔄 Optimized Data Architecture](#-optimized-data-architecture)

## Feature highlights

* Authentication
  * Login
  * Register
* Meals
  * Orders
  * Find the shortest path for meal orders
  * View estimated delivery time
* Map
  * Visualize metro lines currently in service in Paris
* Optimization
  * Algorithms
    * Dijkstra
    * Bellman-Ford
    * Floyd-Warshall
    * A*

## Getting started

### From release (Not Implemented Yet)

Download a release (`.zip` format) and run `LivingParisApp.exe`.

### Build from source code

1. Clone the repository
 
2. Install Microsoft .NET SDK `>=9.x.x`.

3. To run the project, run on `cmd.exe` :

```
run.app [--reset] [--noLogSQL] [--initMock]
```
- Admin's email : `admin`
- Admin's password : `admin`
where :

| Argument | Description |
|----------|-------------|
| `--reset` | Wipe DB. |
| `--initMock` | Wipe DB **and** populate it will new entries|
| `--noLogSQL` | Do not log SQL queries. |

4. To run tests, run on `cmd.exe` :

```
run.test
```

The framework currently in use for testing purposes is *xUnit*.

## Development

### Structure

The codebase is structured as shown below :

```
.
├── config/appsettings.json
├── LivingParisApp/
├── LivingParisApp.Core/
├── LivingParisApp.Services/
├── LivingParisApp.Tests/
├── LivingParisSolution.sln
├── run.app.cmd
└── run.test.cmd
```

where :

| File/directory name | Description |
|---------------------|-------------|
| `config/appsettings.json` | The directory that contains DB credentials and various settings related to the GUI |
| `LivingParisApp/` | The GUI interface and entry point |
| `LivingParisApp.Core/` | The engine/API, and the models |
| `LivingParisApp.Services /` | Interact with the DB, and various utility functions |
| `run.app.cmd` | The helper that runs the project |
| `run.test.cmd` | The helper that runs tests |

### Database

* The RDBMS used in here is MySQL. It is up to the end-user to set up the file `appsettings.json` accordingly.

* Schema (entity-association diagram) :

![Entity Association Diagram](/markdownassets/Image/EA-diagram.png)

## Improvements

### Known issues

* Xaml style is not consistent across tabs.
* Dijkstra is broken
  * *This is not not critical since we are currently using A*.*

### To do

* Tests :
  * Vertex cover
  * Graph coloration

## User Roles

Our platform offers three distinct user roles, each with unique capabilities:

### 👨‍🍳  Chef

As a chef, you can:
- Create custom dishes with personalized ingredients
- List your culinary creations on the marketplace
- Manage incoming orders with flexible control options:
  - Accept and complete orders
  - Refuse orders when necessary
  - Cancel orders when situations change

### 🧑‍💼 Client

As a client, you can:
- Browse the marketplace for dishes from various chefs
- Place orders for multiple dishes across different chefs
- Manage your orders with ease:
  - View detailed order information
  - Track delivery with an interactive Metro Map showing the optimal route from chef to your location
  - Cancel orders when needed

### ⚙️ Admin

Admins have comprehensive system oversight and can:
- Manage all users across the platform
- Control the dish catalog with abilities to:
  - Add, update, or remove dishes
  - Monitor all active orders
  - Access comprehensive statistics and analytics
- Use advanced data tools:
  - Powerful search capabilities
  - Custom filters for efficient data management

## Advanced Features

### 📊 Relationship Visualization
- Interactive network map displaying chef-client interactions
- Color-coded using the Welsh-Powell algorithm for optimal visualization
- Export capability to JSON format for external analysis and visualization

### 🔄 Optimized Data Architecture
Our innovative system architecture:
- Minimizes database queries through client-side API duplication
- Ensures seamless UI updates without excessive database calls
- Maintains data integrity across all interactions
- Creates efficient "save for later" functionality for reconnecting users

**Note:** This optimized design is particularly effective for single-user scenarios, significantly reducing database load while maintaining full functionality.
