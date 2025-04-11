# P3-Consumer Application

A .NET 9.0 console application that implements the consumer side of a producer-consumer video upload system.

## Key Features
- Multiple consumer threads (configurable via input.txt)
- Dynamic TCP port allocation
- Leaky bucket queue implementation
- Docker container support

## Configuration
Create an `input.txt` file with these parameters:
- p=
- c=
- q=

## Docker Configuration
1. Ensure Docker and Docker Compose are installed
2. The container maps ports 9000-9010 to support flexible consumer scaling
3. Run: `docker-compose up`

## Network Notes
- Uses port range mapping (9000-9010) to support dynamic consumer scaling
- Each consumer thread gets its own TCP port starting from 9000
