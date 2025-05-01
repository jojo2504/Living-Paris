# Living-Paris

---

## Code owner
- jojo2504 | Jonathan Tran
  
## Collaborators
- Bahyamin | Benjamin Zilber
- Tibo-7 | Thilbault Thery

## Summary
- [Living-Paris](#living-paris)
  - [Code owner](#code-owner)
  - [Collaborators](#collaborators)
  - [Summary](#summary)
  - [Introduction](#introduction)
  - [Getting Started (For Custom Building)](#getting-started-for-custom-building)
  - [Entity-Relationship Diagram](#entity-relationship-diagram)
  - [Known Issues](#known-issues)
  - [To Implement](#to-implement)

## Introduction
Living Paris is an application that facilitates meal sharing between neighbors within Paris. The platform allows individuals or local businesses to order homemade meals prepared by registered cooks. Users can register as either a cook or a client—or both simultaneously—by providing their personal details such as name, address, phone number, and email.

This project aims to explore graphs and their functionality, particularly in optimizing delivery routes and analyzing user interactions.
**This repository is a project for A2/S4 students in computer sciences.**

## Getting Started (For Custom Building)
1. Clone the repository to your local machine or download the latest release.
2. Make sure your .NET version is at least `9.x.x`.
3. To run the project : 
    - run.app \
      Current arguments list : [`--reset`, `--noLogSQL`, `--initMock`]
      - `reset` wipes all the database's data.
      - `noLogSQL` will prevent any SQL query to be written on the log file located at `%appdata%/livingparis/logs`. 
      - `initMock` resets the database and populate it with arbritary values (not completed).
4. To run the tests :
    - run.test
     
## Entity-Relationship Diagram
![Entity Association Diagram](markdownassets\Image\EA-diagram.png)

## Known Issues
- Xaml style is not consistent across tabs.
- Dijkstra is broken | Not critical since we are using A*.

## To Implement
- Tests for : vertex cover, graph coloration
