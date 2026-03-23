up:
	docker-compose -f Deploy/docker-compose.yml up --build

down:
	docker-compose -f Deploy/docker-compose.yml down

up-d:
	docker-compose -f Deploy/docker-compose.yml up --build -d