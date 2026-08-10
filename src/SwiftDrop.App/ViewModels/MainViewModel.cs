namespace SwiftDrop.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _deviceName = string.Empty;
    private string _deviceId = string.Empty;
    private string _deviceFingerprint = string.Empty;
    private string _receiveFolder = string.Empty;
    private string _remotePeer = string.Empty;
    private string _selectedFile = string.Empty;
    private string _selectedBatch = string.Empty;
    private string _transferStatus = string.Empty;
    private string _batchTransferStatus = string.Empty;
    private string _textTransferStatus = string.Empty;
    private double _transferProgress;
    private double _batchTransferProgress;
    private bool _sendFileEnabled = true;
    private bool _pauseSendEnabled;
    private bool _resumeSendEnabled;
    private bool _cancelSendEnabled;
    private bool _sendBatchEnabled = true;
    private bool _pauseBatchEnabled;
    private bool _resumeBatchEnabled;
    private bool _cancelBatchEnabled;

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value ?? string.Empty);
    }

    public string DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value ?? string.Empty);
    }

    public string DeviceFingerprint
    {
        get => _deviceFingerprint;
        set => SetProperty(ref _deviceFingerprint, value ?? string.Empty);
    }

    public string ReceiveFolder
    {
        get => _receiveFolder;
        set => SetProperty(ref _receiveFolder, value ?? string.Empty);
    }

    public string RemotePeer
    {
        get => _remotePeer;
        set => SetProperty(ref _remotePeer, value ?? string.Empty);
    }

    public string SelectedFile
    {
        get => _selectedFile;
        set => SetProperty(ref _selectedFile, value ?? string.Empty);
    }

    public string SelectedBatch
    {
        get => _selectedBatch;
        set => SetProperty(ref _selectedBatch, value ?? string.Empty);
    }

    public string TransferStatus
    {
        get => _transferStatus;
        set => SetProperty(ref _transferStatus, value ?? string.Empty);
    }

    public string BatchTransferStatus
    {
        get => _batchTransferStatus;
        set => SetProperty(ref _batchTransferStatus, value ?? string.Empty);
    }

    public string TextTransferStatus
    {
        get => _textTransferStatus;
        set => SetProperty(ref _textTransferStatus, value ?? string.Empty);
    }

    public double TransferProgress
    {
        get => _transferProgress;
        set => SetProperty(ref _transferProgress, Math.Clamp(value, 0, 1));
    }

    public double BatchTransferProgress
    {
        get => _batchTransferProgress;
        set => SetProperty(ref _batchTransferProgress, Math.Clamp(value, 0, 1));
    }

    public bool SendFileEnabled
    {
        get => _sendFileEnabled;
        set => SetProperty(ref _sendFileEnabled, value);
    }

    public bool PauseSendEnabled
    {
        get => _pauseSendEnabled;
        set => SetProperty(ref _pauseSendEnabled, value);
    }

    public bool ResumeSendEnabled
    {
        get => _resumeSendEnabled;
        set => SetProperty(ref _resumeSendEnabled, value);
    }

    public bool CancelSendEnabled
    {
        get => _cancelSendEnabled;
        set => SetProperty(ref _cancelSendEnabled, value);
    }

    public bool SendBatchEnabled
    {
        get => _sendBatchEnabled;
        set => SetProperty(ref _sendBatchEnabled, value);
    }

    public bool PauseBatchEnabled
    {
        get => _pauseBatchEnabled;
        set => SetProperty(ref _pauseBatchEnabled, value);
    }

    public bool ResumeBatchEnabled
    {
        get => _resumeBatchEnabled;
        set => SetProperty(ref _resumeBatchEnabled, value);
    }

    public bool CancelBatchEnabled
    {
        get => _cancelBatchEnabled;
        set => SetProperty(ref _cancelBatchEnabled, value);
    }

    public void SetSingleTransferControls(bool sending, bool canResume)
    {
        SendFileEnabled = !sending;
        PauseSendEnabled = sending;
        CancelSendEnabled = sending;
        ResumeSendEnabled = !sending && canResume;
    }

    public void SetBatchTransferControls(bool sending, bool canResume)
    {
        SendBatchEnabled = !sending;
        PauseBatchEnabled = sending;
        CancelBatchEnabled = sending;
        ResumeBatchEnabled = !sending && canResume;
    }
}
