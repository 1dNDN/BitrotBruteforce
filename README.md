Требует .net 8 для работы

```
Commands:
  brute <torrent> <data>                                Найти и исправить битрот
  restore <torrent> <data> <piece-index> <bit-index>    Исправить битрот с известным местом
  extract <torrent> <data> <destination> <piece-index>  Вытащить из торрента часть
  insert <torrent> <data> <destination> <piece-index>   Записать в торрент часть
```


```
Usage:
  Bruteforce brute [<torrent> [<data>]] [options]

Arguments:
  <torrent>  Путь к торрент-файлу
  <data>     Путь к данным

Options:
  -r, --restore   Должна ли утилита самостоятельно восстановить данные
```

```
  Bruteforce restore [<torrent> [<data> [<piece-index> <bit-index>]]] [options]

Arguments:
  <torrent>      Путь к торрент-файлу
  <data>         Путь к данным
  <piece-index>  Номер части (отсчет с нуля)
  <bit-index>    Номер бита (отсчет с нулевого бита части)
```

```
  Bruteforce extract [<torrent> [<data> [<destination> [<piece-index>]]]] [options]

Arguments:
  <torrent>      Путь к торрент-файлу
  <data>         Путь к данным
  <destination>  Путь к файлу с частью
  <piece-index>  Номер части (отсчет с нуля)
```

```
  Bruteforce insert [<torrent> [<data> [<destination> [<piece-index>]]]] [options]

Arguments:
  <torrent>      Путь к торрент-файлу
  <data>         Путь к данным
  <destination>  Путь к файлу с частью
  <piece-index>  Номер части (отсчет с нуля)
```
