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
  - [Getting Started (For Custom Building)](#getting-started-for-custom-building)
  - [Entity-Relationship Diagram](#entity-relationship-diagram-outdated)

## Introduction
Living Paris is an application that facilitates meal sharing between neighbors within Paris. The platform allows individuals or local businesses to order homemade meals prepared by registered cooks. Users can register as either a cook or a client—or both simultaneously—by providing their personal details such as name, address, phone number, and email.

This project aims to explore graphs and their functionality, particularly in optimizing delivery routes and analyzing user interactions.
**This repository is a project for A2/S4 students in computer sciences.**

## Getting Started (For Custom Building)
1. Clone the repository to your local machine or download the latest release.
2. Make sure your .NET version is `9.x`.
3. To run the project : 
    - run.app
      Current arguments list : [`--reset`, `--noLogSQL`, `--initMock`]
      - `reset` wipes all the database's data.
      - `noLogSQL` will prevent any SQL query to be written on the log file. 
      - `initMock` resets the database and populate it.
4. To run the tests :
    - run.test
     
## Entity-Relationship Diagram (Outdated)
![Entity Association Diagram](/markdownassets/Image/Entity_Association_Diagram.png)
This diagram will evolve in the future as we add more functionalities to the app.

## Known Issues
- Cost is suppository for now.
- Marketplace doesn't support dish info view yet.
- Dishes can't be created with ingredients details.
- Sign up infos are not erased after sign up completion.
