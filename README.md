# Living-Paris

---

## Code owner
- jojo2504 | Jonathan Tran
  
## Collaborators
- Bahyamin | Benjamin Zilber
- Tibo-7 | Thilbault Thery

## Summary
- [Living-Paris](#living-paris)
  - [Summary](#summary)
  - [Introduction](#introduction)
  - [Features (On-Progress)](#features-on-progress)
  - [Getting Started (For Custom Building)](#getting-started-for-custom-building)
  - [Entity-Relationship Diagram](#entity-relationship-diagram)

## Introduction
Living Paris is an application that facilitates meal sharing between neighbors within Paris. The platform allows individuals or local businesses to order homemade meals prepared by registered cooks. Users can register as either a cook or a client—or both simultaneously—by providing their personal details such as name, address, phone number, and email.

This project aims to explore graphs and their functionality, particularly in optimizing delivery routes and analyzing user interactions.
**This repository is a project for A2/S4 students in computer sciences.**

## Features (On-Progress)
- Graph Creation: The system models user interactions and delivery routes using graph structures.

- Graph Traversal Algorithms: The application implements Breadth-First Search (BFS) and Depth-First Search (DFS) to analyze and optimize delivery paths.

- XML Representation of Graphs: The graph data is stored and represented using XML for better visualization and management.


## Getting Started (For Custom Building)
1. Clone the repository to your local machine or download the latest release.
2. Make sure your .NET version is `9.0`.
3. To run the project : 
    - go to `LivingParisApp/`,
    - use in terminal `dotnet run` \
      Current arguments list : [`--reset`, `--noLogSQL`]
4. To run the tests :
    - go to `Living-Paris/`,
    - use in terminal `dotnet test`.

## Entity-Relationship Diagram
![Entity Association Diagram](/markdownassets/Image/Entity_Association_Diagram.png)
This diagram will evolve in the future as we add more functionalities to the app.
