# Serverの構築
## Docker
docker build -t udpserver .
## Docker実行
docker run -it --rm -p 11000:11000/udp udpserver

