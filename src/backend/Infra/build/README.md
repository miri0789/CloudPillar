# IoT DICOM hub shared build
## Abstract
The purpose is to have all backend microservices universally built with same parametrized Dockerfile
## Usage
````
cd <project_dir>
docker build -t <acrurl>/my_microservice --build-arg DLL=my_microservice.dll -f ../../infra/build/Dockerfile .
docker push <acrurl>/my_microservice
````

For example, for PoC D2C processor:
````
docker build -t d2c-processor --build-arg DLL=d2c-processor.dll -f ../../infra/build/Dockerfile .
````