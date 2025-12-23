FROM python:3.13.11

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY firmware_update_microservice.py ./

CMD ["python", "./firmware-broker.py"]
