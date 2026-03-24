up:
	docker-compose -f docker-compose.dev.yml up --build

down:
	docker-compose -f docker-compose.dev.yml down

up-d:
	docker-compose -f docker-compose.dev.yml up --build -d