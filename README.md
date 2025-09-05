This application is a sandbox application for viewing flight data. The data is fake, and is used for demonstration purposes only.
This app supports a user creating reports based on the flight data.

Tech stack:
- .NET 8 backend
- React frontend
- Hardcoded JSON database with dummy flight data, that is pulled from the backend.

API endpoints:
- GET /api/flights - Get all flights
- GET /api/reports - Get all reports
- POST /api/reports - Create a new report
- GET /api/reports/{id} - Get a report by ID
- PUT /api/reports/{id} - Update a report by ID
- DON'T IMPLEMENT DELETE yet

# Running
Backend:
1. cd backend/FlightApi
2. dotnet run --urls http://localhost:5073

Frontend (in another terminal):
1. cd frontend
2. npm run dev
3. Open http://localhost:5173

Ensure backend CORS allows the frontend origin (configured for http://localhost:5173).