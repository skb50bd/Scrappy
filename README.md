# Scrappy

## Description

Scrappy is a web scraping project designed to scrape content from various websites.
It consists of multiple components, including a Console UI, a Crawler, and a Downloader.
The project is built using C# and is designed to be highly modular and scalable.

## Table of Contents

- Description
- Usage
- Contributing

### Prerequisites

- Docker

### Steps

- Clone the repository:

    ```shell
    git clone https://github.com/skb50bd/Scrappy.git
    ```

- Navigate to the project directory:

    ```shell
    cd Scrappy
    ```

- Build the docker images:

    ```shell
    docker compose build
    ```

- Run the Docker Compose file to set up RabbitMQ:

    ```shell
    docker compose up rabbitmq -d
    ```

- Run the Docker Compose file to set up the Scrappy Crawler:

    ```shell
    docker compose up crawler -d
    ```

- Run the Docker Compose file to set up the Scrappy Downloader:

    ```shell
    docker compose up downloader -d
    ```

- The files will be stored in **`docker-volumes/scrappy-downloader-data/`**

## Contributing

If you'd like to contribute, please fork the repository and use a feature branch. Pull requests are warmly welcome.
