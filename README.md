Требует .net 8 для работы

```
Commands:
  brute <torrent> <data>                                Найти и исправить битрот
  restore <torrent> <data> <piece-index> <bit-index>    Исправить битрот с известным местом
  extract <torrent> <data> <destination> <piece-index>  Вытащить из торрента часть
  insert <torrent> <data> <destination> <piece-index>   Записать в торрент часть
  brutepiece <pieceBinPath>                             Сбрутить готовый блоб с известным хешем
  bruteprepare <torrent> <data> <destination>           Сгенерировать файлы с блобами для последующего восстановления
```


```
Usage:
  Bruteforce brute [<torrent> [<data>]] [options]

Arguments:
  <torrent>  Путь к торрент-файлу
  <data>     Путь к данным

Options:
  -r, --restore   Должна ли утилита самостоятельно восстановить данные
  -t, --threads <threads>  Сколько потоков использовать, по умолчанию все доступные [default: Environment.ProcessorCount]
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

```
Description:
  Сбрутить готовый блоб с известным хешем

Usage:
  Bruteforce brutepiece <pieceBinPath> [options]

Arguments:
  <pieceBinPath>  Путь к блобу части с именем в формате brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute

Options:
  -r, --restore            Должна ли утилита самостоятельно восстановить данные
  -t, --threads <threads>  Сколько потоков использовать, по умолчанию все доступные [default: Environment.ProcessorCount]
```

```
Description:
  Сгенерировать файлы с блобами для последующего восстановления

Usage:
  Bruteforce bruteprepare [<torrent> [<data> <destination>]] [options]

Arguments:
  <torrent>      Путь к торрент-файлу
  <data>         Путь к данным
  <destination>  Путь к папке с частями
```
