# FastMediator Unit Tests

Questo progetto contiene i test unitari per la libreria FastMediator.

## Esecuzione dei test

Per eseguire i test, seguire questi passaggi:

1. Eseguire tutti i test:
   ```bash
   dotnet test
   ```

2. Eseguire i test con report dettagliato:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

3. Eseguire solo test specifici (esempio per i test di validazione):
   ```bash
   dotnet test --filter DisplayName~ValidationTests
   ```

## Struttura dei test

I test sono organizzati in diverse classi che coprono le principali funzionalità della libreria:

1. **DispatcherTests**: Test delle funzionalità di base del dispatcher.
   - Invio di richieste sincrone e asincrone
   - Pubblicazione di notifiche
   - Verifica che gli handler vengano chiamati correttamente

2. **ValidationTests**: Test del sistema di validazione.
   - Validazione delle richieste
   - Gestione degli errori di validazione
   - Raccolta di errori multipli

3. **PipelineBehaviorTests**: Test dei behavior della pipeline.
   - Ordine di esecuzione corretto dei behavior
   - Behavior ordinati con diversi livelli di priorità
   - Interazione tra i behavior e gli handler

## Modalità di approccio ai test

I test seguono un approccio AAA (Arrange-Act-Assert):

- **Arrange**: Configurazione dell'ambiente di test e delle dipendenze
- **Act**: Esecuzione dell'operazione da testare
- **Assert**: Verifica che i risultati siano quelli attesi

## Estensione dei test

Per aggiungere nuovi test:

1. Creare una nuova classe di test per una funzionalità specifica.
2. Implementare metodi di test con l'attributo `[Fact]` per test senza parametri o `[Theory]` con `[InlineData]` per test parametrizzati.
3. Utilizzare i metodi di assertion di xUnit e FluentAssertions per verificare i risultati.

## Best Practices

1. Ogni test dovrebbe verificare un singolo aspetto della funzionalità.
2. I test devono essere indipendenti e non influenzarsi a vicenda.
3. Utilizzare dati di test significativi.
4. Evitare la logica condizionale nei test.
5. Includere assert sia per i casi positivi che negativi.
6. Per i behavior e altri componenti di infrastruttura, utilizzare mock quando appropriato.