### Как установить
1) Добавить [базовый](https://github.com/EveryGameSPlay/Package-Unity-Types) пакет. ``` https://github.com/EveryGameSPlay/Package-Unity-Types.git ```

2) Добавить данный пакет. ``` https://github.com/EveryGameSPlay/Package-Unity-SceneManager.git ```

[Инструкция установки](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
## Общее
Данный пакет содержит в себе менеджер сцен, используемый мной при разработке приложений на Unity. Функционал менеджера позволяет с удобством работать как с одной сценой, так и в режиме мульти-сцен. Он также обрабатывает нюансы загрузки сцен с разными режимами (single, additive).

Особенности:
- Теги
- Список сцен
- Передача данных
- Конструктор запросов 
- Ожидание выполнения запросов
- Настройка порядка выполнения запросов

## Примеры использования
### Загрузка
Пример загрузки двух сцен:
```f#
SceneManager.LoadScene("first", mode: Additive).With("second")
    .Apply();
```
### Параллельность
Пример параллельной загрузки двух сцен:
```f#
SceneManager.LoadScene("first", mode: Additive).With("second")
    .Parallel().Apply();
```
### Ожидание
Пример ожидания окончания загрузки:
```f#
SceneManager.LoadScene("first", mode: Additive).With("second")
    .OnComplete(x=>Debug.Log("completed all"))
    .Apply()
```
Окончание загрузки запроса будет вызвано только после выполнения этого и всех подзапросов. Это значит, что пока все сцены в запросе не будут загружены, событие не активируется. Таким образом мы всегда будет уверены в загрузке сцены и дополнительных сцен. Работает и с параллельным режимом загрузки.

### Теги
Пример добавления тега сцене:
```f#
SceneManager.LoadScene("first", mode: Additive).With("second").Tag("gameplay","systems")
    .Apply()
```
Теги в последствии могут быть получены при обращении к сцене. Удобно для обозначения функционала сцен в коде. Теги применяются только к корневому запросу.

### Вложенность
Вложенные запросы могут быть отредактированы отдельно. Также на них не распространяются изменения более высокоуровневых запросов, если это не обозначено.

Пример редактирования вложенного запроса:
```f#
SceneManager.LoadScene("first", mode: Additive)
    .With("second", x => x
        .Tag("hud")
        .OnComplete(x=>Debug.Log("completed inner")))

    .Tag("gameplay")
    .OnComplete(x=>Debug.Log(completed root))
    .Apply()
```
Метод **Apply()** нужно вызывать только на корневом запросе.
### Данные
Пример передачи данных:
```f#
SceneManager.LoadScene("first", mode: Additive)
    .With("second", x => x
        .With("third")
        .Data(someObject, dataForAllScenes: true))
        
    .Data(rootObject, dataForAllScenes: false)
    .Apply()

first  - rootObject
second - someObject
third  - someObject
```

Пример получения данных:
```f#
Option<DataType> data = SceneManager.GetData<DataType>(scene: gameObject.scene);
```
### Выгрузка
При выгрузке также можно создавать запросы и подзапросы. Выгрузка осуществляется по названию сцены, по экземпляру, по коллекции или по тегу.
```f#
SceneManager.UnloadScene(sceneInstance).With("second").Apply();
SceneManager.UnloadScene(sceneCollection).Apply();
SceneManager.UnloadByTag(tag: "sometag");
```
### Дополнительно
Пример получения списка сцен:
```f#
var scenes = SceneManager.Scenes;
```
Пример получения всех сцен с тегом:
```f#
var taggedScenes = SceneManager.GetScenesByTag(tag: "sometag"); 
```
Пример получения информации о сцене:
```f#
Option<SceneInfo> sceneInfo = SceneManager.GetSceneInfo(scene: sceneInstance);
```

## Лицензия
Используется MIT License.

## Контакты
Kirill Gasanov - gasanov.kirill.dev@gmail.com
