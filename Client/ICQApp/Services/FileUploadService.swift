import Foundation

actor FileUploadService {
    static let shared = FileUploadService()

    func upload(fileURL: URL, token: String) async throws -> [String: Any] {
        // multipart upload to /api/files/upload
        throw URLError(.unsupportedURL) // implement with URLSession uploadTask
    }
}
